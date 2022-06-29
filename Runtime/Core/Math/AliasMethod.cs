using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace wakuwaku.Core
{

    public class AliasMethod
    {
        public void Init(List<double> distribution)
        {
            table_ = new List<TableItem>(distribution.Count);

            Queue<int> bigger_than_one = new Queue<int>();
            Queue<int> small_than_one = new Queue<int>();
            {
                int idx = 0;

                double sum = 0;

                distribution.ForEach(x => { sum += x; });
                double integral = sum / distribution.Count;
                distribution.ForEach(x =>
                {
                    table_.Add(new TableItem() { alias = idx, prob = x / sum, pdf = x / integral, u = distribution.Count * x / sum });
                    if (table_[idx].u >= 1)
                        bigger_than_one.Enqueue(idx++);
                    else
                        small_than_one.Enqueue(idx++);
                    if (double.IsInfinity(table_[idx - 1].pdf) || double.IsInfinity(table_[idx - 1].pdf)
                        )
                    {
                        int a1 = 1;
                    }
                });

            }

            {
                double integral = 0;
                table_.ForEach(x => { integral += x.pdf / distribution.Count; });
                Debug.Assert(Math.Abs(integral - 1) < 1e-3f);

            }


            while (bigger_than_one.Count != 0 && small_than_one.Count != 0)
            {
                int idx_bigger = bigger_than_one.Dequeue();
                //                 if (table_[idx_bigger].u == 1 || small_than_one.Count == 0)
                //                 {
                //                     table_[idx_bigger].alias = -1;
                //                     table_[idx_bigger].prob = 1;
                //                     continue;
                //                 }
                int idx_smaller = small_than_one.Dequeue();

                table_[idx_bigger].u -= 1 - table_[idx_smaller].u;

                table_[idx_smaller].alias = idx_bigger;

                if (table_[idx_bigger].u > 1)
                    bigger_than_one.Enqueue(idx_bigger);
                else if (table_[idx_bigger].u < 1)
                {
                    small_than_one.Enqueue(idx_bigger);
                }
            }

            foreach (var item in bigger_than_one)
            {
                table_[item].u = 1.0f;
            }
            foreach (var item in small_than_one)
            {
                table_[item].u = 1.0f;
            }
        }

        public double PDF(int idx)
        {
            return table_[idx].pdf;
        }
        public int Sample()
        {
            var idx = UnityEngine.Random.Range(0, table_.Count - 1);
            if (UnityEngine.Random.Range(0.0f, 1.0f) > table_[idx].u)
            {
                idx = table_[idx].alias;
            }
            return idx;
        }
        public class TableItem
        {
            public double pdf;
            public int alias;
            public double u;
            public double prob;
        }

        public List<TableItem> table_;
    }


}
