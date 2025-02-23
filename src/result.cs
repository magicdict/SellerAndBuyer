using System;
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
    public double hope_score { get; set; }

    public static string GetHope(Buyer buyer, Goods goods)
    {
        string rtn = "";
        if (buyer.第一意向.hopeType != enmHope.无 && goods.IsMatchHope(buyer.第一意向)) rtn += "1-";
        if (buyer.第二意向.hopeType != enmHope.无 && goods.IsMatchHope(buyer.第二意向)) rtn += "2-";
        if (buyer.第三意向.hopeType != enmHope.无 && goods.IsMatchHope(buyer.第三意向)) rtn += "3-";
        if (buyer.第四意向.hopeType != enmHope.无 && goods.IsMatchHope(buyer.第四意向)) rtn += "4-";
        if (buyer.第五意向.hopeType != enmHope.无 && goods.IsMatchHope(buyer.第五意向)) rtn += "5-";
        if (rtn == "") return "0";
        return rtn.TrimEnd("-".ToCharArray());
    }

    internal Result Clone()
    {
        return this.MemberwiseClone() as Result;
    }

    public static double Score(Buyer buyer)
    {
        return buyer.HopeScore * 0.6 + buyer.Diary_score * 0.4;
    }

    public static double Score(List<Result> results, List<Buyer> buyers_Breed)
    {
        int total_cnt = buyers_Breed.Sum(x => x.购买货物数量);
        double diary_score = buyers_Breed.Sum(x => x.Diary_score * x.购买货物数量 / total_cnt) * 0.4;
        double hope_score = buyers_Breed.Sum(x => x.HopeScore * x.购买货物数量 / total_cnt) * 0.6;
        //按照卖方进行GroupBy，然后Distinct仓库号
        System.Console.WriteLine("==============================================================");
        System.Console.WriteLine("总体贸易分单数：" + results.Count);
        System.Console.WriteLine("==============================================================");
        System.Console.WriteLine("仓库得分：" + diary_score);
        System.Console.WriteLine("意向得分：" + hope_score);
        double RealScore = buyers_Breed.Sum(x => x.Score * x.购买货物数量 / total_cnt);
        System.Console.WriteLine("得分率：" + RealScore);
        System.Console.WriteLine("==============================================================");
        return RealScore;
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
        sr.Close();
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
    public static void OnlyOutPut(string path, string strKbn, List<Buyer> buyers, string midname)
    {
        //测评
        var rs = new List<Result>();
        foreach (var buyer in buyers)
        {
            rs.AddRange(buyer.results);
        }
        Result.WriteToCSV(path + strKbn + "_" + midname + ".csv", rs);
    }

    public static List<Seller> CheckScoreOutput(string path, string strKbn, List<Buyer> buyers, string midname)
    {
        //测评
        var rs = new List<Result>();
        foreach (var buyer in buyers)
        {
            rs.AddRange(buyer.results);
        }
        buyers = Buyer.ReadBuyerFile(path + "buyer.csv").Where(x => x.品种 == strKbn).ToList(); ;
        var sellers = Seller.ReadSellerFile(path + "seller.csv").Where(x => x.品种 == strKbn).ToList(); ;
        Summary.CheckResult(rs, buyers, sellers);   //检查结果
        Result.Score(rs, buyers);           //计算得分
        Result.WriteToCSV(path + strKbn + "_" + midname + ".csv", rs);
        return sellers;
    }

    public static void CompressResultFile(string filename)
    {
        var rs = ReadFromCSV(filename);
        System.Console.WriteLine("Before Count:" + rs.Count);
        var grp = rs.GroupBy(x => x.买方客户 + x.仓库 + x.卖方客户 + x.品种 + x.货物编号);
        var rs_compress = new List<Result>();
        foreach (var item in grp)
        {
            var r = item.First();
            r.分配货物数量 = item.Sum(x => x.分配货物数量);
            rs_compress.Add(r);
        }
        System.Console.WriteLine("After Count:" + rs_compress.Count);
        WriteToCSV(filename , rs_compress);
    }
}