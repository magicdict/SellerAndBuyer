using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

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
    public const string strSR = "SR";
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

    static ConcurrentDictionary<string, int> strHopeDic = new ConcurrentDictionary<string, int>();


    public static int ConvertHopeStr2Score(string Hope, string kbn)
    {
        if (Hope == "0") return 0;
        if (strHopeDic.ContainsKey(Hope + kbn)) return strHopeDic[Hope + kbn];
        int score = 0;
        var hopes = Hope.Split("-").ToList();
        if (kbn == Utility.strCF)
        {
            if (hopes.Contains("1")) score += 33;
            if (hopes.Contains("2")) score += 27;
            if (hopes.Contains("3")) score += 20;
            if (hopes.Contains("4")) score += 13;
            if (hopes.Contains("5")) score += 7;
        }
        else
        {
            if (hopes.Contains("1")) score += 40;
            if (hopes.Contains("2")) score += 30;
            if (hopes.Contains("3")) score += 20;
            if (hopes.Contains("4")) score += 10;
        }
        strHopeDic.TryAdd(Hope + kbn, score);
        return score;
    }

    

}

