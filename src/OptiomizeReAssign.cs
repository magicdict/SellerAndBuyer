using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
public static partial class Optiomize
{
    public static void ReAssignFirstHope(string path, string resultfilename, string strKbn)
    {
        var buyers = Buyer.ReadBuyerFile(path + "buyer.csv").Where(x => x.品种 == strKbn).ToList(); ;
        var sellers = Seller.ReadSellerFile(path + "seller.csv").Where(x => x.品种 == strKbn).ToList(); ;
        var rs = Result.ReadFromCSV(path + resultfilename);
        var rs_buyers = rs.GroupBy(x => x.买方客户);

        //明细和买家绑定
        Parallel.ForEach(
            rs_buyers, grp =>
            {
                var buyer = buyers.Where(x => x.买方客户 == grp.Key).First();
                buyer.results = grp.ToList();
                buyer.fill_results_hopescore();
            }
        );
        //必须做绑定之后做
        Result.Score(rs, buyers);

        //货物属性字典的建立
        Goods.GoodsDict.Clear();
        var goods_grp = sellers.GroupBy(x => x.货物编号);
        foreach (var item in goods_grp)
        {
            Goods.GoodsDict.Add(item.Key, item.First());
        }

        //第一意向GroupBy
        var first_hope_grp = buyers.GroupBy(x => x.第一意向);
        Parallel.ForEach(first_hope_grp, grp =>
        {
            var IsRun = true;
            if (grp.Key.hopeType == enmHope.无) IsRun = false;
            //CF只运行产地：新疆
            //if (strKbn == "CF" && !(grp.Key.hopeType == enmHope.产地 && grp.Key.hopeValue == "新疆")) IsRun = false;
            if (IsRun)
            {
                //先把已经满足分配的人全部挑选出来
                var satisfy = grp.ToList().Where(x => x.IsFirstHopeSatisfy).ToList();
                System.Console.WriteLine("Start:" + grp.Key.hopeType + "-" + grp.Key.hopeValue + "(" + satisfy.Count() + ")");
                var total_goods_quantities = satisfy.Sum(x => x.购买货物数量);
                var score_before = satisfy.Sum(x => x.Score * x.购买货物数量 / total_goods_quantities);
                //制作Clone对象
                var Satisfy_Clone = new List<Buyer>();
                var sellers = new List<Seller>();
                foreach (var buyer in satisfy)
                {
                    Satisfy_Clone.Add(buyer.Clone());
                    foreach (var r in buyer.results)
                    {
                        sellers.Add(new Seller(r));
                    }
                }
                //重新打散之后重排
                ReAssign(Satisfy_Clone, sellers);
                var score_after = Satisfy_Clone.Sum(x => x.Score * x.购买货物数量 / total_goods_quantities);
                if (score_after > score_before)
                {
                    //替换
                    foreach (Buyer buyer_c in Satisfy_Clone)
                    {
                        var buyer = buyers.Where(x => x.买方客户 == buyer_c.买方客户).First();
                        buyer.results = buyer_c.results;
                    }
                    System.Console.WriteLine("Up:" + (score_after-score_before));
                }
                System.Console.WriteLine("End:" + grp.Key.hopeType + "-" + grp.Key.hopeValue);
                Satisfy_Clone = null;
                sellers = null;
                System.GC.Collect();
            }
        });
        Result.CheckScoreOutput(path, strKbn, buyers, "ReHope");
    }

    private static void ReAssign(List<Buyer> buyers, List<Seller> sellers)
    {
        //按照最大意向值进行排序
        var sellers_remain = sellers;
        buyers.Sort((x, y) =>
        {
            if (x.TotalHopeScore != y.TotalHopeScore)
            {
                return y.TotalHopeScore - x.TotalHopeScore;
            }
            else
            {
                return y.购买货物数量.CompareTo(x.购买货物数量);
            }
        });
        int cnt = 0;
        foreach (var buyer in buyers)
        {
            buyer.results = PreAssign.AssignItemWithRepo(buyer, sellers_remain);
            buyer.fill_results_hopescore();
            sellers_remain = sellers_remain.Where(x => !x.是否分配完毕).ToList();
            cnt++;
            if (buyers.Count > 5000 && (cnt % 100 == 0))
                System.Console.WriteLine(cnt + "/" + buyers.Count);
        }
    }
}