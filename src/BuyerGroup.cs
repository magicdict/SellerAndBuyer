using System.Collections.Generic;
using System.Linq;

public class BuyerGroup
{

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
        get { return Goods.RemainDict[hope]; }
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


    public static System.Comparison<BuyerGroup> BuyerMinRare = (x, y) =>
    {
        Buyer b_x = x.EvaluateBuyer();
        Buyer b_y = y.EvaluateBuyer();
        return b_x.MinRare.CompareTo(b_y.MinRare);
    };

    public static System.Comparison<BuyerGroup> BuyerAvgRare = (x, y) =>
    {
        //SR:79.453647333334
        Buyer b_x = x.EvaluateBuyer();
        Buyer b_y = y.EvaluateBuyer();
        return b_x.AvgRare.CompareTo(b_y.AvgRare);
    };

    public static System.Comparison<BuyerGroup> BuyerComboRare = (x, y) =>
    {
        Buyer b_x = x.EvaluateBuyer();
        Buyer b_y = y.EvaluateBuyer();
        return b_x.ComboRare.CompareTo(b_y.ComboRare);
    };

    public static System.Comparison<BuyerGroup> Evalute_Best = (x, y) =>
    {
        //SR:79.30704
        //CF:75.71092
        return x.SupportNeedRate.CompareTo(y.SupportNeedRate);
    };


    public static System.Comparison<BuyerGroup> BuyerAvgRare2 = (x, y) =>
    {
        //SR:79.44453933333409
        Buyer b_x = x.EvaluateBuyer();
        Buyer b_y = y.EvaluateBuyer();
        return (b_x.AvgRare + x.SupportNeedRate).CompareTo(b_y.AvgRare +  + y.SupportNeedRate);
    };       

    public static System.Comparison<BuyerGroup> BuyerMix = (x, y) =>
    {
        //SR:
        Buyer b_x = x.EvaluateBuyer();
        Buyer b_y = y.EvaluateBuyer();
        return (b_x.AvgRare * 0.7 + b_x.ComboRare * 0.2 + b_x.MinRare * 0.1 )
               .CompareTo(b_y.AvgRare * 0.7 + b_y.ComboRare * 0.2 + b_y.MinRare * 0.1 );
    };       


}