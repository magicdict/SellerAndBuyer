using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
public static partial class Optiomize
{
    public static void OptiomizeRepo(string path, string resultfilename, string strKbn)
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

        System.Console.WriteLine("1个仓库：" + buyers.Count(x => x.RepoCnt == 1));
        System.Console.WriteLine("2个仓库：" + buyers.Count(x => x.RepoCnt == 2));
        System.Console.WriteLine("3个仓库：" + buyers.Count(x => x.RepoCnt == 3));
        System.Console.WriteLine("4个仓库：" + buyers.Count(x => x.RepoCnt == 4));

        //将无意向的单仓库的记录和2仓库的进行交换
        var buyer_nohope_1Repo = buyers.Where(x => x.TotalHopeScore == 0).ToList();
        //最普遍的2仓库的处理,符合意向约束的记录
        var buyers_2Repo_FirstHope = buyers.Where(x => x.RepoCnt == 2 && x.IsFirstHopeSatisfy).ToList();

        System.Console.WriteLine("提供货物数量:" + buyer_nohope_1Repo.Sum(x => x.购买货物数量));
        System.Console.WriteLine("接受货物数量:" + buyers_2Repo_FirstHope.Sum(x => x.购买货物数量));

        //按照仓库失分率排序
        buyers_2Repo_FirstHope.Sort((x, y) =>
        {
            return y.购买货物数量 - x.购买货物数量;
        });
        //开始交换
        //STEP1.单一意向，受到时间约束的记录,在交换仓库的同时，尽量不要造成意向分下滑
        foreach (var buyer in buyers_2Repo_FirstHope)
        {
            var repos = buyer.results.Select(x => x.仓库).Distinct().ToList();
            var repo0 = repos[0];
            var repo1 = repos[1];
            var repo0_quantity = buyer.results.Where(x => x.仓库 == repo0).Sum(x => x.分配货物数量);
            var repo1_quantity = buyer.results.Where(x => x.仓库 == repo1).Sum(x => x.分配货物数量);
            var repo_main = repo0_quantity > repo1_quantity ? repo0 : repo1;
            var repo_repl_quantity = repo0_quantity > repo1_quantity ? repo1_quantity : repo0_quantity;

            //寻找相同仓库，
            var buyers_support = buyer_nohope_1Repo.Where(x => x.MainRepo == repo_main).ToList();
            var rs_supper = new List<Result>();
            foreach (var item in buyers_support)
            {
                rs_supper.AddRange(item.results);
            }
            var remain_quantity = rs_supper.Sum(x =>
            {
                var seller = Goods.GoodsDict[x.货物编号];
                return seller.IsMatchHope(buyer.第一意向) ? x.分配货物数量 : 0;
            });
            //待交换不足
            if (remain_quantity < repo_repl_quantity) continue;
            //按照满意度排序
            rs_supper.Sort((x, y) =>
            {
                var seller_x = Goods.GoodsDict[x.货物编号];
                var seller_y = Goods.GoodsDict[y.货物编号];
                return seller_y.GetHopeScore(buyer).CompareTo(seller_x.GetHopeScore(buyer));
            });

            var score_before = buyer.Score;
            var buyer_need_Clone = buyer.Clone();
            var buyers_Support_Clone = new List<Buyer>();
            var rs_repl = buyer_need_Clone.results.Where(x => x.仓库 != repo_main).ToList();
            //除去待交换的记录
            buyer_need_Clone.results = buyer_need_Clone.results.Where(x => x.仓库 == repo_main).ToList();
            //rs已经做过合并操作，可以寻找到原始记录
            foreach (var r in rs)
            {
                //由于已经在上面限制了
                if (r.分配货物数量 <= repo_repl_quantity)
                {
                    //需要全部的记录
                }
                else
                {

                }
                if (repo_repl_quantity == 0) break;
            }
        }
    }
}