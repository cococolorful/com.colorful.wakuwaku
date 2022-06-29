using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace  wakuwaku.Function.WRenderPipeline
{
    public abstract class RGReflectionUtil
    {
        public static string GetLastNameOfType(Type type)
        {
            if (type == null)return null;
            return type.Name;
        }
        public static Type GetTypeFromName(string typeName)
        {
            Type type = null;
            Assembly[] assemblyArray = AppDomain.CurrentDomain.GetAssemblies();
            int assemblyArrayLength = assemblyArray.Length;
            for (int i = 0; i < assemblyArrayLength; ++i)
            {
                type = assemblyArray[i].GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            for (int i = 0; (i < assemblyArrayLength); ++i)
            {
                Type[] typeArray = assemblyArray[i].GetTypes();
                int typeArrayLength = typeArray.Length;
                for (int j = 0; j < typeArrayLength; ++j)
                {
                    if (typeArray[j].Name.Equals(typeName))
                    {
                        return typeArray[j];
                    }
                }
            }
            return type;
        }
        public static bool IsEngineObject(Type type)
        {
            return type.BaseType == typeof(UnityEngine.Object) || type.BaseType.BaseType == typeof(UnityEngine.Object);
        }
        // inputs,outputs,parameters
        public static Tuple<List<FieldInfo>, List<FieldInfo>, List<FieldInfo>> GetPublicField(Type type)
        {
            Tuple<List<FieldInfo>, List<FieldInfo>, List<FieldInfo>> fieldInfos = new Tuple<List<FieldInfo>, List<FieldInfo>, List<FieldInfo>>(new List<FieldInfo>(),new List<FieldInfo>(),new List<FieldInfo>());

            foreach (var fieldInfo in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if(fieldInfo.IsPublic)
                {
                    var pinInfo = fieldInfo.GetCustomAttribute<IPass.PortPinAttribute>();
                    if (pinInfo != null)
                    {
                        if (pinInfo.pinType != IPass.PinType.Read)
                            fieldInfos.Item2.Add(fieldInfo);
                        if (pinInfo.pinType != IPass.PinType.Write)
                            fieldInfos.Item1.Add(fieldInfo);
                    }
                    else if (fieldInfo.FieldType == typeof(RendererListHandle) ||
                        fieldInfo.FieldType == typeof(TextureHandle) ||
                        fieldInfo.FieldType == typeof(ComputeBufferHandle))
                    {
                        continue;
                    }
                    else
                    {
                        if (fieldInfo.GetCustomAttribute<NonSerializedAttribute>() != null)
                            continue;
                        fieldInfos.Item3.Add(fieldInfo);
                    }
                }
                else if(fieldInfo.IsPrivate)
                {
                    if(fieldInfo.GetCustomAttribute<SerializeField>() != null)
                        fieldInfos.Item3.Add(fieldInfo);
                }
            }
            return fieldInfos;
        }
    }
}