using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Windows;
using wakuwaku.Core;

namespace wakuwaku.Function.WRenderPipeline
{
    public class EnvironmentalUtil
    {
        public static void DealSkyBox(WRenderPipelineAsset asset, UnityEngine.Object tex)
        {
            if (((tex) as Texture).dimension != UnityEngine.Rendering.TextureDimension.Tex2D)
                Debug.Log("sky need to be texture 2D");

            var sky_box = (tex as Texture2D);
            var col = sky_box.GetPixels(0);
            int sum_pixels = sky_box.width * sky_box.height;

            double Y(Color radiance)
            {
                //0.2126.r, 0.7152f, 0.0722f
                return radiance.r * 0.2126f + radiance.g * 0.7152f + radiance.b * 0722f;
            }

            List<double> distribution = new List<double>(sky_box.width * sky_box.height);
            double sum = 0;
            for (int y = 0; y < sky_box.height; y++)
            {
                float v = Math.Clamp((y + 0.5f) / (float)sky_box.height, 0, 1);
                double sin_theta = Math.Sin(Math.PI * v);
                for (int x = 0; x < sky_box.width; x++)
                {
                    //UV2SolidAngle((float)i / sky_box.width, (float)j / sky_box.height);
                    float u = Math.Clamp((x + 0.5f) / sky_box.width, 0, 1);

                    var c = sky_box.GetPixelBilinear(u, v);

                    int idx = y * sky_box.width + x;
                    distribution.Add(Math.Abs(Y(c.linear) * sin_theta));
                    sum += distribution[idx];
                }
            }
            double integral_distribution = sum / distribution.Count;
            for (int i = 0; i < distribution.Count; i++)
            {
                //distribution[i] = Math.Max(0,distribution[i] - integral_distribution); 
            }

            if (asset.sky_box_pdf_ != null)
                asset.sky_box_pdf_.Release();
            asset.sky_box_pdf_ = new ComputeBuffer(sum_pixels, 4);
            if (asset.sky_box_sampling_prob_ != null)
                asset.sky_box_sampling_prob_.Release();

            asset.sky_box_sampling_prob_ = new ComputeBuffer(sum_pixels, 4);
            if (asset.sky_box_sampling_alias_ != null)
                asset.sky_box_sampling_alias_.Release();
            asset.sky_box_sampling_alias_ = new ComputeBuffer(sum_pixels, 4);

            AliasMethod table = new AliasMethod();
            table.Init(distribution);

            var pdfs = new List<float>();
            var pdfs_alias_prob = new List<float>();
            var pdfs_alias_alias = new List<int>();
            table.table_.ForEach(x => { pdfs.Add((float)x.pdf); pdfs_alias_prob.Add((float)x.u); pdfs_alias_alias.Add(x.alias); });

            DebugIntoFile(sky_box, col, table);

            asset.sky_box_pdf_.SetData(pdfs);
            asset.sky_box_sampling_prob_.SetData(pdfs_alias_prob);
            asset.sky_box_sampling_alias_.SetData(pdfs_alias_alias);
            Shader.SetGlobalBuffer("g_sky_box_pdf", asset.sky_box_pdf_);
            Shader.SetGlobalBuffer("g_sky_box_sampling_prob", asset.sky_box_sampling_prob_);
            Shader.SetGlobalBuffer("g_sky_box_sampling_alias", asset.sky_box_sampling_alias_);
            Shader.SetGlobalTexture("g_sky_box", asset.sky_box_tex);
        }

        private static void DebugIntoFile(Texture2D sky_box, Color[] col, AliasMethod table)
        {
#if UNITY_EDITOR
            var sky_box_sub = new Texture2D(sky_box.width, sky_box.height, GraphicsFormat.R32G32B32A32_SFloat, TextureCreationFlags.None);

            for (int j = 0; j < sky_box.height; j++)
            {
                for (int i = 0; i < sky_box.width; i++)
                {
                    int idx = j * sky_box.width + i;
                    sky_box_sub.SetPixel(i, j, col[idx]);

                }
            }

            int spp = 50000;
            while (spp-- > 0)
            {
                var sample_idx = table.Sample();
                Color s = new Color((float)table.table_[sample_idx].prob, (float)table.table_[sample_idx].pdf, (float)table.table_[sample_idx].prob);
                sky_box_sub.SetPixel((sample_idx % sky_box.width), (sample_idx / sky_box.width), s);
            }

            byte[] bytes = sky_box_sub.EncodeToEXR();
            File.WriteAllBytes(Application.dataPath + "/../SavedScreen.exr", bytes);
#endif
        }
    }
}
