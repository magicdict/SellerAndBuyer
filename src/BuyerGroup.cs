using System.Collections.Generic;
using System.Linq;

public class BuyerGroup
{
    /// <summary>
    /// 各个意向剩余量
    /// </summary>
    public Dictionary<(enmHope, string), int> RemainDict;
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
    (enmHope, string) hope = (enmHope.无, string.Empty);
    public BuyerGroup((enmHope, string) Hope, List<Buyer> Buyers)
    {
        hope = Hope;
        Buyers.Sort((x, y) =>
        {
            return y.平均持仓时间.CompareTo(x.平均持仓时间);
        });
        for (int i = 0; i < Buyers.Count; i++)
        {
            lines.Push(Buyers[i]);
        }
    }

    public static List<Seller> Sellers_Remain;

    public static System.Comparison<BuyerGroup> Evalute_1 = (x, y) =>
    {
        int x_total = x.EvaluateBuyer().TotalHopeScore;
        int y_total = y.EvaluateBuyer().TotalHopeScore;
        double x_total_rate = x.EvaluateBuyer().TotalHopeSatisfyRate(Sellers_Remain);
        double y_total_rate = y.EvaluateBuyer().TotalHopeSatisfyRate(Sellers_Remain);
        if (x_total != y_total)
        {
            //可获得最大意向值升序
            //CF：73.68198
            return y_total.CompareTo(x_total);
        }
        else
        {
            if (x_total_rate != y_total_rate)
            {
                return y_total_rate.CompareTo(x_total_rate);
            }
            else
            {
                //供求比降序
                return x.SupportNeedRate.CompareTo(y.SupportNeedRate);
            }
        }
    };

    public static System.Comparison<BuyerGroup> Evalute_Best = (x, y) =>
       {
           //SR:79.30704
           //CF:73.72274
           return x.SupportNeedRate.CompareTo(y.SupportNeedRate);
       };
}