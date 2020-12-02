using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public static class Adjust
{
    public static void Run(string path)
    {
        var rs = Result.ReadFromCSV(path + "result.csv");
        //按照卖家信息GroupBy
        var rs_CF = rs.Where(x => x.品种 == "CF").ToList();
        var rs_SR = rs.Where(x => x.品种 == "SR").ToList();

        var buyers = Buyer.ReadBuyerFile(path + "buyer.csv");


        System.Console.WriteLine("开始优化CF数据：");
        Optiomize(rs_CF, buyers.Where(x => x.品种 == "CF").ToList());
        System.Console.WriteLine("开始优化SR数据：");
        Optiomize(rs_SR, buyers.Where(x => x.品种 == "SR").ToList());
    }

    static void Optiomize(List<Result> rs, List<Buyer> buyer)
    {
        var strKb = rs.First().品种;
        var quantityDict = new ConcurrentDictionary<string, int>();
        Parallel.ForEach(
            rs, r =>
            {
                //区分已经在上层过滤了
                int quantity = 0;
                if (quantityDict.ContainsKey(r.买方客户))
                {
                    quantity = quantityDict[r.买方客户];
                }
                else
                {
                    quantity = buyer.Where(x => x.买方客户 == r.买方客户).First().购买货物数量;
                    quantityDict.TryAdd(r.买方客户, quantity);
                }
                r.hope_score = Utility.GetHopeScore(r.对应意向顺序, strKb) * r.分配货物数量 / quantity;
            }
        );
        Result.Score(rs, buyer);
        var rs_buyers = rs.GroupBy(x => x.买方客户);
        //使用多个仓库的买家数
        var multi_repo_cnt = rs_buyers.Count(x => x.ToList().Select(x => x.仓库).Distinct().Count() > 1);
        System.Console.WriteLine("使用多个仓库的买家数：" + multi_repo_cnt);
        //得分为0的记录数
        var hope_zero_point = rs.Where(x => x.对应意向顺序 == "0").Sum(x => x.分配货物数量);
        System.Console.WriteLine("意向为0的货物数：" + hope_zero_point);
        //在不违背第一意向的前提下，是否能够进行货物兑换提升意向分？
    }
}