using System.Collections.Generic;
using System.Linq;

public class BuyerGroup
{
    /// <summary>
    /// 各个意向剩余量
    /// </summary>
    /// <param name="RemainDict"></param>
    /// <typeparam name="(enmHope"></typeparam>
    /// <typeparam name="string)"></typeparam>
    /// <typeparam name="int"></typeparam>
    public static Dictionary<(enmHope, string), int> RemainDict = new Dictionary<(enmHope, string), int>();
    /// <summary>
    /// 更新剩余量字典
    /// </summary>
    /// <param name="seller"></param>
    /// <param name="quantity"></param>
    public static void RefreshRemainDict(Seller seller, int quantity)
    {
        foreach (var item in RemainDict)
        {
            if (seller.IsMatchHope(item.Key)) RemainDict[item.Key] -= quantity;
        }
    }

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

    public Buyer GetBuyer()
    {
        return lines.Pop();
    }

    private int match_count
    {
        get { return RemainDict[hope]; }
    }

    public int RemainBuyerCnt{
        get{
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
}