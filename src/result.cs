using System.Collections.Generic;
using System.IO;

public class Result
{
    const string header = "买方客户,卖方客户,品种,货物编号,仓库,分配货物数量,对应意向顺序";
    public string 买方客户 { get; set; }
    public string 卖方客户 { get; set; }
    public string 品种 { get; set; }
    public string 货物编号 { get; set; }
    public string 仓库 { get; set; }
    public int 分配货物数量 { get; set; }
    public string 对应意向顺序 { get; set; }
    public int hope_score { get; set; }

    public static string GetHope(Buyer buyer, Seller seller)
    {
        string rtn = "";
        if (seller.IsMatchHope(buyer.第一意向)) rtn += "1-";
        if (seller.IsMatchHope(buyer.第二意向)) rtn += "2-";
        if (seller.IsMatchHope(buyer.第三意向)) rtn += "3-";
        if (seller.IsMatchHope(buyer.第四意向)) rtn += "4-";
        if (seller.IsMatchHope(buyer.第五意向)) rtn += "5-";
        if (rtn == "") return "0";
        return rtn.TrimEnd("-".ToCharArray());
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
    public static void AppendToCSV(string filename, List<Result> results)
    {
        var isNeedTitle = !System.IO.File.Exists(filename);
        var sw = new StreamWriter(filename, true, System.Text.Encoding.GetEncoding("GB2312"));
        if (isNeedTitle) sw.WriteLine(header);
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