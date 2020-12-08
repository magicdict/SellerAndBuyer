using System.Collections.Generic;
using System.Linq;

public class BuyerGroup
{
    /// <summary>
    /// 各个意向剩余量
    /// </summary>
    public Dictionary<(enmHope hopeType, string hopeValue), int> RemainDict;
    Stack<Buyer> lines = new Stack<Buyer>();
    /// <summary>
    /// 供求比例
    /// </summary>
    public double SupportNeedRate
    {
        get
        {
            if (IsFinished) return double.MaxValue;
            return (double)match_count / lines.Sum(x => x.剩余货物数量);
        }
    }

    public double TotalHopeScore_AVG
    {
        get
        {
            if (IsFinished) return double.MaxValue;
            return lines.Average(x => x.TotalHopeScore);
        }
    }


    public Buyer GetBuyer()
    {
        return lines.Pop();
    }

    public Buyer EvaluateBuyer()
    {
        return lines.Peek();
    }

    private int match_count
    {
        get { return RemainDict[hope]; }
    }

    public int RemainBuyerCnt
    {
        get
        {
            return lines.Count;
        }
    }

    /// <summary>
    /// 无法或者无需分配了
    /// </summary>
    /// <value></value>
    public bool IsFinished
    {
        get
        {
            if (match_count == 0) return true;
            return lines.Count == 0;
        }
    }
    (enmHope hopeType, string hopeValue) hope = (enmHope.无, string.Empty);
    public BuyerGroup((enmHope hopeType, string hopeValue) Hope, List<Buyer> Buyers)
    {
        hope = Hope;
        Buyers.Sort((x, y) =>
        {
            return x.平均持仓时间.CompareTo(y.平均持仓时间);
        });
        for (int i = 0; i < Buyers.Count; i++)
        {
            lines.Push(Buyers[i]);
        }
    }


    public static System.Comparison<BuyerGroup> Evalute_1 = (x, y) =>
    {
        return y.RemainBuyerCnt.CompareTo(x.RemainBuyerCnt);
    };
    public static System.Comparison<BuyerGroup> Evalute_2 = (x, y) =>
       {
           //SR:79.30704
           //CF:73.72274
           return y.TotalHopeScore_AVG.CompareTo(x.TotalHopeScore_AVG);
       };
    public static System.Comparison<BuyerGroup> Evalute_Best = (x, y) =>
       {
           //SR:79.30704
           //CF:73.72274
           return x.SupportNeedRate.CompareTo(y.SupportNeedRate);
       };
}