
using System.Collections.Generic;

public record Goods
{
    public static Dictionary<string,Goods> GoodsDict = new Dictionary<string, Goods>();
    public string 品种 { get; set; }
    public string 货物编号 { get; set; }
    //仓库
    public string 仓库 { get; set; }
    //品牌
    public string 品牌 { get; set; }
    //产地
    public string 产地 { get; set; }
    //年度
    public string 年度 { get; set; }
    //等级
    public string 等级 { get; set; }
    //类别
    public string 类别 { get; set; }


    public int GetHopeScore(Buyer buyer)
    {
        int score = 0;
        if (品种 == Utility.strCF)
        {
            if (IsMatchHope(buyer.第一意向)) score += 33;
            if (IsMatchHope(buyer.第二意向)) score += 27;
            if (IsMatchHope(buyer.第三意向)) score += 20;
            if (IsMatchHope(buyer.第四意向)) score += 13;
            if (IsMatchHope(buyer.第五意向)) score += 7;
        }
        else
        {
            if (IsMatchHope(buyer.第一意向)) score += 40;
            if (IsMatchHope(buyer.第二意向)) score += 30;
            if (IsMatchHope(buyer.第三意向)) score += 20;
            if (IsMatchHope(buyer.第四意向)) score += 10;
        }
        return score;
    }

    public bool IsMatchHope((enmHope, string) hope, bool EmptyAsTrue = false)
    {
        switch (hope.Item1)
        {
            case enmHope.仓库:
                return 仓库.Equals(hope.Item2);
            case enmHope.品牌:
                return 品牌.Equals(hope.Item2);
            case enmHope.产地:
                return 产地.Equals(hope.Item2);
            case enmHope.年度:
                return 年度.Equals(hope.Item2);
            case enmHope.等级:
                return 等级.Equals(hope.Item2);
            case enmHope.类别:
                return 类别.Equals(hope.Item2);
            default:
                //意向为无时，无条件满足
                return EmptyAsTrue;
        }
    }

}