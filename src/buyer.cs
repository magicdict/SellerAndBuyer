using System.Collections.Generic;
using System.IO;
using System.Linq;

public class Buyer
{
    public string 买方客户 { get; set; }
    public int 平均持仓时间 { get; set; }
    public int 购买货物数量 { get; set; }
    public string 品种 { get; set; }
    public (enmHope, string) 第一意向 { get; set; }
    public (enmHope, string) 第二意向 { get; set; }
    public (enmHope, string) 第三意向 { get; set; }
    public (enmHope, string) 第四意向 { get; set; }
    public (enmHope, string) 第五意向 { get; set; }

    public Dictionary<string, int> Seller_Buyer_HopeScoreDic = new Dictionary<string, int>(1024);

    public int GetHopeScore(Seller seller)
    {
        if (Seller_Buyer_HopeScoreDic.ContainsKey(seller.货物编号)) return Seller_Buyer_HopeScoreDic[seller.货物编号];
        int score = 0;
        if (品种 == Utility.strCF)
        {
            if (seller.IsMatchHope(第一意向)) score += 33;
            if (seller.IsMatchHope(第二意向)) score += 27;
            if (seller.IsMatchHope(第三意向)) score += 20;
            if (seller.IsMatchHope(第四意向)) score += 13;
            if (seller.IsMatchHope(第五意向)) score += 7;
        }
        else
        {
            if (seller.IsMatchHope(第一意向)) score += 40;
            if (seller.IsMatchHope(第二意向)) score += 30;
            if (seller.IsMatchHope(第三意向)) score += 20;
            if (seller.IsMatchHope(第四意向)) score += 10;
        }
        Seller_Buyer_HopeScoreDic.Add(seller.货物编号, score);
        return score;
    }

    public int TotalHopeScore
    {
        get
        {
            int score = 0;
            if (品种 == Utility.strCF)
            {
                if (this.第一意向.Item1 != enmHope.无) score += 33;
                if (this.第二意向.Item1 != enmHope.无) score += 27;
                if (this.第三意向.Item1 != enmHope.无) score += 20;
                if (this.第三意向.Item1 != enmHope.无) score += 13;
                if (this.第五意向.Item1 != enmHope.无) score += 7;
            }
            else
            {
                if (this.第一意向.Item1 != enmHope.无) score += 40;
                if (this.第二意向.Item1 != enmHope.无) score += 30;
                if (this.第三意向.Item1 != enmHope.无) score += 20;
                if (this.第三意向.Item1 != enmHope.无) score += 10;
            }
            return score;
        }
    }

    public int 已分配货物数量 { get; set; }

    public int 剩余货物数量
    {
        get
        {
            return 购买货物数量 - 已分配货物数量;
        }
    }
    public bool 是否分配完毕
    {
        get
        {
            return 购买货物数量 == 已分配货物数量;
        }
    }

    /// <summary>
    /// 具体供货信息明细
    /// </summary>
    /// <value></value>
    public List<Result> results { get; set; }

    private bool is_filled_results_hopescore = false;

    /// <summary>
    /// 计算每单意向分数
    /// </summary>
    public void fill_results_hopescore()
    {
        foreach (var r in results)
        {
            r.hope_score = Utility.ConvertHopeStr2Score(r.对应意向顺序, 品种) * r.分配货物数量 / 购买货物数量;
        }
        is_filled_results_hopescore = true;
    }

    public int Result_HopeScore
    {
        get
        {
            if (!is_filled_results_hopescore) fill_results_hopescore();
            return results.Sum(x => x.hope_score);
        }
    }

    public static List<Buyer> ReadBuyerFile(string filename)
    {
        var buyers = new List<Buyer>();
        var sr = new StreamReader(filename, System.Text.Encoding.GetEncoding("GB2312"));
        sr.ReadLine();  //去除标题
        while (!sr.EndOfStream)
        {
            var infos = sr.ReadLine().Split(",");
            buyers.Add(new Buyer()
            {
                买方客户 = infos[0],
                平均持仓时间 = int.Parse(infos[1]),
                购买货物数量 = int.Parse(infos[2]),
                品种 = infos[3],
                第一意向 = string.IsNullOrEmpty(infos[4]) ? (enmHope.无, "") : (Utility.GetHope(infos[4]), infos[5]),
                第二意向 = string.IsNullOrEmpty(infos[6]) ? (enmHope.无, "") : (Utility.GetHope(infos[6]), infos[7]),
                第三意向 = string.IsNullOrEmpty(infos[8]) ? (enmHope.无, "") : (Utility.GetHope(infos[8]), infos[9]),
                第四意向 = string.IsNullOrEmpty(infos[10]) ? (enmHope.无, "") : (Utility.GetHope(infos[10]), infos[11]),
                第五意向 = string.IsNullOrEmpty(infos[12]) ? (enmHope.无, "") : (Utility.GetHope(infos[12]), infos[13]),
            });
        }
        sr.Close();
        System.Console.WriteLine("买家件数：" + buyers.Count);
        return buyers;
    }

    #region Sort

    public static System.Comparison<Buyer> Hope_1st_comparison = (x, y) =>
    {
        if (y.平均持仓时间 != x.平均持仓时间)
        {
            return y.平均持仓时间.CompareTo(x.平均持仓时间);
        }
        else
        {
            return Hope_comparison(x, y);
        }
    };


    public static System.Comparison<Buyer> Hope_comparison = (x, y) =>
    {
        //意向分多的先分配，购物数少的先分配
        if (x.TotalHopeScore != y.TotalHopeScore)
        {
            return y.TotalHopeScore.CompareTo(x.TotalHopeScore);
        }
        else
        {
            return x.购买货物数量.CompareTo(y.购买货物数量);
        }
    };

    #endregion
}