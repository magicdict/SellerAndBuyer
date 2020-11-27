public enum enmHope
{
    无,
    仓库,
    产地,
    等级,
    类别,
    年度,
    品牌
}

public static class Utility
{

    public const string strCF = "CF";

    public static enmHope GetHope(string str)
    {
        switch (str)
        {
            case "仓库": return enmHope.仓库;
            case "产地": return enmHope.产地;
            case "等级": return enmHope.等级;
            case "类别": return enmHope.类别;
            case "年度": return enmHope.年度;
            case "品牌": return enmHope.品牌;
            default:
                throw new System.Exception("未知意向：" + str);
        }
    }

    public static int GetHopeScore(Buyer buyer, Seller seller)
    {
        int score = 0;
        if (buyer.品种 == Utility.strCF)
        {
            if (seller.IsMatchHope(buyer.第一意向)) score += 33;
            if (seller.IsMatchHope(buyer.第二意向)) score += 27;
            if (seller.IsMatchHope(buyer.第三意向)) score += 20;
            if (seller.IsMatchHope(buyer.第四意向)) score += 13;
            if (seller.IsMatchHope(buyer.第五意向)) score += 7;
        }
        else
        {
            if (seller.IsMatchHope(buyer.第一意向)) score += 40;
            if (seller.IsMatchHope(buyer.第二意向)) score += 30;
            if (seller.IsMatchHope(buyer.第三意向)) score += 20;
            if (seller.IsMatchHope(buyer.第四意向)) score += 10;
        }
        return score;
    }

}

