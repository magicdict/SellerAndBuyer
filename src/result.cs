using System.Collections.Generic;
using System.IO;
using System.Linq;

public class Result
{
    public string 买方客户 { get; set; }
    public string 卖方客户 { get; set; }
    public string 品种 { get; set; }
    public string 货物编号 { get; set; }
    public string 仓库 { get; set; }
    public int 分配货物数量 { get; set; }
    public string 对应意向顺序 { get; set; }
    public int hope_score { get; set; }

    public static string GetHope(Buyer buyer, Goods goods)
    {
        string rtn = "";
        if (buyer.第一意向.Item1 != enmHope.无 && goods.IsMatchHope(buyer.第一意向)) rtn += "1-";
        if (buyer.第二意向.Item1 != enmHope.无 && goods.IsMatchHope(buyer.第二意向)) rtn += "2-";
        if (buyer.第三意向.Item1 != enmHope.无 && goods.IsMatchHope(buyer.第三意向)) rtn += "3-";
        if (buyer.第四意向.Item1 != enmHope.无 && goods.IsMatchHope(buyer.第四意向)) rtn += "4-";
        if (buyer.第五意向.Item1 != enmHope.无 && goods.IsMatchHope(buyer.第五意向)) rtn += "5-";
        if (rtn == "") return "0";
        return rtn.TrimEnd("-".ToCharArray());
    }

    public static double Score(List<Result> results, Buyer buyer)
    {
        results.Sort((x, y) => { return (x.买方客户 + x.卖方客户).CompareTo(y.买方客户 + y.卖方客户); });
        int hope_score = results.Sum(x => x.hope_score);
        //按照卖方进行GroupBy，然后Distinct仓库号
        var repo = results.GroupBy(x => x.买方客户).Select(y => new { 品种 = y.First().品种, 仓库数 = y.Select(z => z.仓库).Distinct().Count() });
        int Diary_score = repo.Sum(x =>
        {
            int score = 100;
            if (x.品种 == Utility.strCF)
            {
                score -= (x.仓库数 - 1) * 20;
            }
            else
            {
                score -= (x.仓库数 - 1) * 25;
            }
            return score;
        });
        int score = (int)(hope_score * 0.6 + Diary_score * 0.4);
        return score;
    }

    public static double Score(List<Result> results, List<Buyer> buyers_Breed)
    {
        results.Sort((x, y) => { return (x.买方客户 + x.卖方客户).CompareTo(y.买方客户 + y.卖方客户); });
        int hope_score = results.Sum(x => x.hope_score);
        //按照卖方进行GroupBy，然后Distinct仓库号
        var repo = results.GroupBy(x => x.买方客户).Select(y => new { 品种 = y.First().品种, 仓库数 = y.Select(z => z.仓库).Distinct().Count() });
        int Diary_score = repo.Sum(x =>
        {
            int score = 100;
            if (x.品种 == Utility.strCF)
            {
                score -= (x.仓库数 - 1) * 20;
            }
            else
            {
                score -= (x.仓库数 - 1) * 25;
            }
            return score;
        });
        int score = (int)(hope_score * 0.6 + Diary_score * 0.4);
        System.Console.WriteLine("==============================================================");
        System.Console.WriteLine("总体贸易分单数：" + results.Count);
        System.Console.WriteLine("==============================================================");
        System.Console.WriteLine("获得意向分数：" + hope_score);
        int totalhopescore = buyers_Breed.Sum(x => x.TotalHopeScore);
        System.Console.WriteLine("最大意向分数：" + totalhopescore);
        System.Console.WriteLine("意向得分率：" + (hope_score * 100 / totalhopescore) + "%");
        System.Console.WriteLine("==============================================================");
        System.Console.WriteLine("记录分数：" + Diary_score);
        System.Console.WriteLine("最大记录分数：" + 100 * buyers_Breed.Count);
        System.Console.WriteLine("记录得分率：" + (Diary_score / buyers_Breed.Count) + "%");
        System.Console.WriteLine("==============================================================");
        System.Console.WriteLine("总体分数：" + score);
        System.Console.WriteLine("标准分数：" + (double)score / buyers_Breed.Count);
        int score_stardard = buyers_Breed.Count * 100;
        System.Console.WriteLine("得分率：" + (score * 100 / score_stardard) + "%");
        System.Console.WriteLine("==============================================================");
        return score;
    }


    /// <summary>
    /// 读取初排结果
    /// </summary>
    public static List<Result> ReadFromCSV(string filename)
    {
        var sr = new StreamReader(filename, System.Text.Encoding.GetEncoding("GB2312"));
        sr.ReadLine();  //去除标题
        var results = new List<Result>();
        while (!sr.EndOfStream)
        {
            var info = sr.ReadLine().Split(",".ToCharArray());
            var r = new Result();
            r.买方客户 = info[0];
            r.卖方客户 = info[1];
            r.品种 = info[2];
            r.货物编号 = info[3];
            r.仓库 = info[4];
            r.分配货物数量 = int.Parse(info[5]);
            r.对应意向顺序 = info[6];
            results.Add(r);
        }
        return results;
    }
    public static void WriteToCSV(string filename, List<Result> results)
    {
        var sw = new StreamWriter(filename, false, System.Text.Encoding.GetEncoding("GB2312"));
        string header = "买方客户,卖方客户,品种,货物编号,仓库,分配货物数量,对应意向顺序";
        sw.WriteLine(header);
        foreach (var result in results)
        {
            sw.Write(result.买方客户 + ",");
            sw.Write(result.卖方客户 + ",");
            sw.Write(result.品种 + ",");
            sw.Write(result.货物编号 + ",");
            sw.Write(result.仓库 + ",");
            sw.Write(result.分配货物数量 + ",");
            sw.WriteLine(result.对应意向顺序);
        }
        sw.Close();
    }

}