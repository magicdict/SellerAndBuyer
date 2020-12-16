
using System.Collections.Generic;
using System.Linq;

public record Goods
{
    public static Dictionary<string, Goods> GoodsDict = new Dictionary<string, Goods>();
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

    public bool IsMatchHope((enmHope hopeType, string hopeValue) hope, bool EmptyAsTrue = false)
    {
        switch (hope.hopeType)
        {
            case enmHope.仓库:
                return 仓库.Equals(hope.hopeValue);
            case enmHope.品牌:
                return 品牌.Equals(hope.hopeValue);
            case enmHope.产地:
                return 产地.Equals(hope.hopeValue);
            case enmHope.年度:
                return 年度.Equals(hope.hopeValue);
            case enmHope.等级:
                return 等级.Equals(hope.hopeValue);
            case enmHope.类别:
                return 类别.Equals(hope.hopeValue);
            default:
                //意向为无时，无条件满足
                return EmptyAsTrue;
        }
    }
    /// <summary>
    /// 各个意向剩余量
    /// </summary>
    public static Dictionary<(enmHope hopeType, string hopeValue), int> RemainDict = new Dictionary<(enmHope hopeType, string hopeValue), int>();
    public static Dictionary<(enmHope hopeType, string hopeValue), int> GlobalNeedDict = new Dictionary<(enmHope hopeType, string hopeValue), int>();
    public static Dictionary<(enmHope hopeType, string hopeValue), int> GlobalSupportDict = new Dictionary<(enmHope hopeType, string hopeValue), int>();

    public static Dictionary<(enmHope hopeType, string hopeValue), double> GlobalSupportNeedRateDict = new Dictionary<(enmHope hopeType, string hopeValue), double>();

    public static void Init(string path, string strKb)
    {
        var sellers = Seller.ReadSellerFile(path + "seller.csv");
        var buyers = Buyer.ReadBuyerFile(path + "buyer.csv");
        var sellers_Breed = sellers.Where(x => x.品种 == strKb).ToList();
        var buyers_Breed = buyers.Where(x => x.品种 == strKb).ToList();
        //货物属性字典的建立
        Goods.GoodsDict.Clear();
        var goods_grp = sellers.GroupBy(x => x.货物编号);
        foreach (var item in goods_grp)
        {
            Goods.GoodsDict.Add(item.Key, item.First());
        }

        GlobalNeedDict.Clear();
        GlobalSupportDict.Clear();
        GlobalSupportNeedRateDict.Clear();
        foreach (var buyer in buyers)
        {
            var hopes = new (enmHope, string)[] { buyer.第一意向, buyer.第二意向, buyer.第三意向, buyer.第四意向, buyer.第五意向 };
            foreach (var hope in hopes)
            {
                if (hope.Item1 != enmHope.无)
                {
                    if (!GlobalNeedDict.ContainsKey(hope))
                    {
                        GlobalNeedDict.Add(hope, 0);
                        GlobalSupportDict.Add(hope, 0);
                        GlobalSupportNeedRateDict.Add(hope, 0);
                    }
                    GlobalNeedDict[hope] += buyer.购买货物数量;
                }
            }
        }
        foreach (var seller in sellers)
        {
            foreach (var support in GlobalSupportDict)
            {
                if (seller.IsMatchHope(support.Key)) GlobalSupportDict[support.Key] += seller.货物数量;
            }
        }
        foreach (var rate in GlobalSupportNeedRateDict)
        {
            GlobalSupportNeedRateDict[rate.Key] = (double)GlobalSupportDict[rate.Key] / GlobalNeedDict[rate.Key];
        }
    }
}