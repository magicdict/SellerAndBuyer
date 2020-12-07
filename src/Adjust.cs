using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
public static class Adjust
{
    public static void Run(string path, string resultfilename)
    {
        var rs = Result.ReadFromCSV(path + resultfilename);
        //按照卖家信息GroupBy
        var rs_CF = rs.Where(x => x.品种 == Utility.strCF).ToList();
        var rs_SR = rs.Where(x => x.品种 == Utility.strSR).ToList();

        var buyers = Buyer.ReadBuyerFile(path + "buyer.csv");
        var selleers = Seller.ReadSellerFile(path + "seller.csv");

        var buyer_CF = buyers.Where(x => x.品种 == Utility.strCF).ToList();
        var buyer_SR = buyers.Where(x => x.品种 == Utility.strSR).ToList();

        var sellers_CF = selleers.Where(x => x.品种 == Utility.strCF).ToList();
        var sellers_SR = selleers.Where(x => x.品种 == Utility.strSR).ToList();

        if (rs_SR.Count != 0)
        {
            System.Console.WriteLine("开始检查SR数据：");
            if (CheckResult(rs_SR, buyer_SR, sellers_SR))
                Result.Score(rs_SR, buyer_SR);
        }
        if (rs_CF.Count != 0)
        {
            System.Console.WriteLine("开始检查CF数据：");
            if (CheckResult(rs_CF, buyer_CF, sellers_CF))
                Result.Score(rs_CF, buyer_CF);
        }
        Result.WriteToCSV(path + resultfilename, rs);
    }

    static bool CheckResult(List<Result> rs, List<Buyer> buyers, List<Seller> sellers)
    {
        var rs_buyers = rs.GroupBy(x => x.买方客户);
        var isOk = true;
        //意向顺序标记错误
        if (buyers.First().品种 != "CF")
        {
            System.Console.WriteLine("意向顺序标记");
            Parallel.ForEach(
                rs, r =>
                {
                    var seller = sellers.Find(x => x.卖方客户 == r.卖方客户 && x.货物编号 == r.货物编号);
                    var buyer = buyers.Where(x => x.买方客户 == r.买方客户).First();
                    var rightOrder = Result.GetHope(buyer, seller);
                    if (rightOrder != r.对应意向顺序)
                    {
                        r.对应意向顺序 = rightOrder;
                        isOk = false;
                    }
                }
            );
        }
        else
        {
            System.Console.WriteLine("CF数据太多，意向顺序标记检查 SKIP");
        }
        if (!isOk)
        {
            isOk = true;
            System.Console.WriteLine("意向顺序标记错误,Fixed！");
        }
        System.Console.WriteLine("分配记录数和买家货物数");
        Parallel.ForEach(
            rs_buyers, grp =>
            {
                var buyer = buyers.Where(x => x.买方客户 == grp.Key).First();
                buyer.results = grp.ToList();
                buyer.fill_results_hopescore();
                if (buyer.购买货物数量 != buyer.results.Sum(x => x.分配货物数量))
                {
                    System.Console.WriteLine(buyer.买方客户 + ":分配记录数和买家货物数不匹配");
                    isOk = false;
                }
            }
        );
        if (!isOk) return false;
        System.Console.WriteLine("买家货物和分配记录数");
        Parallel.ForEach(
            buyers, buyer =>
            {
                if (buyer.购买货物数量 != buyer.results.Sum(x => x.分配货物数量))
                {
                    System.Console.WriteLine(buyer.买方客户 + ":买家货物和分配记录数不匹配");
                    isOk = false;
                }
            }
        );

        if (!isOk) return false;

        //检查一下买家货物数，卖家货物数，明细货物数是否相同
        var total_buyer = buyers.Sum(x => x.购买货物数量);
        var total_seller = sellers.Sum(x => x.货物数量);
        var total_Result = rs.Sum(x => x.分配货物数量);
        if (total_Result != total_buyer)
        {
            System.Console.WriteLine("分配记录数和买家货物数不匹配");
            return false;
        }
        if (total_seller != total_buyer)
        {
            System.Console.WriteLine("卖家数和买家货物数不匹配");
            return false;
        }
        //按照第一意向GroupBy
        var first_hoep_grp = buyers.GroupBy(x => x.第一意向).ToList();
        foreach (var grp in first_hoep_grp)
        {
            if (grp.Key.Item1 == enmHope.无) continue;
            //按照持仓时间排序
            var same_hope_buyers = grp.ToList();
            same_hope_buyers.Sort((x, y) => { return y.平均持仓时间.CompareTo(x.平均持仓时间); });
            var isMatch = true;
            foreach (var buyer in same_hope_buyers)
            {
                var isSingleAllMatch = true;
                foreach (var r in buyer.results)
                {
                    if (!r.对应意向顺序.StartsWith("1"))
                    {
                        isSingleAllMatch = false;
                        break;
                    }
                }
                if (isSingleAllMatch)
                {
                    //整条记录都满足
                    if (!isMatch)
                    {
                        System.Console.WriteLine(buyer.第一意向.Item1 + ":" + buyer.第一意向.Item2);
                        return false;
                    }
                }
                else
                {
                    //出现了不满足的情况
                    if (isMatch)
                    {
                        isMatch = false;
                    }
                }
            }
        }
        return true;
    }

    static void Optiomize(List<Result> rs, List<Buyer> buyers, List<Seller> sellers)
    {
        var rs_buyers = rs.GroupBy(x => x.买方客户);
        //使用多个仓库的买家数
        var multi_repo_cnt = rs_buyers.Count(x => x.ToList().Select(x => x.仓库).Distinct().Count() > 1);
        System.Console.WriteLine("使用多个仓库的买家数：" + multi_repo_cnt);
        //得分为0的记录数
        var hope_zero_point = rs.Where(x => x.对应意向顺序 == "0").Sum(x => x.分配货物数量);
        System.Console.WriteLine("意向得分为0的货物数：" + hope_zero_point);

        Result.Score(rs, buyers);

        //无意向的记录
        var buyer_without_hope = buyers.Where(x => x.TotalHopeScore == 0).ToList();
        var rs_for_exchange = new List<Result>();
        foreach (var buyer in buyer_without_hope)
        {
            rs_for_exchange.AddRange(buyer.results);
        }
        System.Console.WriteLine("无意向的买家数：" + buyer_without_hope.Count);
        System.Console.WriteLine("无意向的买家记录明细数：" + rs_for_exchange.Count);
        System.Console.WriteLine("无意向的买家记录货物数量：" + buyer_without_hope.Sum(x => x.购买货物数量));

        //需要进行交换的货物记录
        var buyer_need_exchange = buyers.Where(x => x.TotalHopeScore != 0 && x.Result_HopeScore == 0);
        var GoodNumDict = new Dictionary<string, Seller>();
        foreach (var buyer in buyer_need_exchange)
        {
            foreach (var result in rs_for_exchange)
            {
                if (!GoodNumDict.ContainsKey(result.货物编号))
                {
                    GoodNumDict.Add(result.货物编号, sellers.Where(x => result.货物编号 == x.货物编号).First());
                }
                var s = GoodNumDict[result.货物编号];
                if (buyer.GetHopeScore(s) != 0)
                {
                    var 出让方_分配货物数量 = result.分配货物数量;
                    break;
                }
            }
        }
    }
}