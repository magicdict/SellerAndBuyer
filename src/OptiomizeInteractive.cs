using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
public static partial class Optiomize
{
    #region OptiomizeInteractive
    public static void OptiomizeInteractive(string path, string resultfilename, string strKbn)
    {
        var buyers = Buyer.ReadBuyerFile(path + "buyer.csv").Where(x => x.品种 == strKbn).ToList(); ;
        var sellers = Seller.ReadSellerFile(path + "seller.csv").Where(x => x.品种 == strKbn).ToList(); ;
        var rs = Result.ReadFromCSV(path + resultfilename);
        var rs_buyers = rs.GroupBy(x => x.买方客户);
        //使用多个仓库的买家数
        var multi_repo_cnt = rs_buyers.Count(x => x.ToList().Select(x => x.仓库).Distinct().Count() > 1);
        System.Console.WriteLine("使用多个仓库的买家数：" + multi_repo_cnt);
        //得分为0的记录数
        var hope_zero_point = rs.Where(x => x.对应意向顺序 == "0").Sum(x => x.分配货物数量);
        System.Console.WriteLine("意向得分为0的货物数：" + hope_zero_point);
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
        var beforeScore = Result.Score(rs, buyers);

        //货物属性字典的建立
        Goods.GoodsDict.Clear();
        var goods_grp = sellers.GroupBy(x => x.货物编号);
        foreach (var item in goods_grp)
        {
            Goods.GoodsDict.Add(item.Key, item.First());
        }

        //每种第一意向，满足的最新持仓时间的记录
        var MinHoldTime = new Dictionary<(enmHope hopeType, string hopeValue), int>();
        var first_hope_group = buyers.GroupBy(x => x.第一意向);
        foreach (var item in first_hope_group)
        {
            if (item.Key.hopeType == enmHope.无) continue;
            var grp = item.Where(x => !x.IsFirstHopeSatisfy).ToList();  //所有没有全部满足第一意向的记录
            if (grp.Count == 0)
            {
                MinHoldTime.Add(item.Key, -1);
                continue;
            }
            grp.Sort((x, y) => { return y.平均持仓时间.CompareTo(x.平均持仓时间); });
            //寻找到第一个意向没有全部满足的记录
            MinHoldTime.Add(item.Key, grp.First().平均持仓时间);
        }

        //标记第一意向被锁定的记录
        foreach (var item in buyers)
        {
            if (item.第一意向.hopeType != enmHope.无)
            {
                //MinHoldTime表示从这个时间开始就出现不满足的记录，所以，这里取 > ,如果是 = 的话，也是不用约束的
                if (item.平均持仓时间 > MinHoldTime[item.第一意向]) item.IsLockFirstHope = true;
            }
        }

        //单一仓库  无第一意向 有第一意向，不完美 
        var buyer_target = buyers.Where(x => x.RepoCnt == 1).ToList();
        //System.Console.WriteLine(buyer_target,count);
        buyer_target = buyers.Where(
            x => (x.TotalHopeScore == 0) ||
                (x.TotalHopeScore != 0 && !x.IsAllHopeSatisfied)
        ).ToList();

        //大规模交换
        int UpScore = 0;
        for (int need_idx = 0; need_idx < buyer_target.Count; need_idx++)
        {
            if (need_idx % 10 == 0)
                System.Console.WriteLine("need_idx：" + need_idx + " UpScore：" + UpScore.ToString());
            if (need_idx % 1000 == 0){
                Result.CheckScoreOutput(path, strKbn, buyers, "Inter_" + UpScore.ToString());
            }
            var buyer_need = buyer_target[need_idx];
            for (int support_idx = need_idx + 1; support_idx < buyer_target.Count; support_idx++)
            {
                if (need_idx == support_idx) continue;   //自己不和自己交换
                var buyer_support = buyer_target[support_idx];
                buyer_support.Seller_Buyer_HopeScoreDic = new Dictionary<string, int>();
                buyer_need.Seller_Buyer_HopeScoreDic = new Dictionary<string, int>(); ;
                var up = IsExchangeBuyerResult(buyer_need, buyer_support);
                if (up != 0)
                {
                    UpScore += up;
                    if (UpScore % 100 == 0)
                        System.Console.WriteLine("Up Score：" + UpScore);
                }
            }
        }
        Result.CheckScoreOutput(path, strKbn, buyers, "Inter");
    }



    /// <summary>
    /// 是否进行交换
    /// </summary>
    /// <param name="buyer_need"></param>
    /// /// <param name="buyer_support"></param>
    private static int IsExchangeBuyerResult(Buyer buyer_need, Buyer buyer_support)
    {
        //非完美的记录都重排
        if (buyer_need.IsPerfectScore && buyer_support.IsPerfectScore) return 0;
        var total_goods_quantities = buyer_need.购买货物数量 + buyer_support.购买货物数量;
        //var before = buyer_need.Score * (buyer_need.购买货物数量 / total_goods_quantities) +
        //             buyer_support.Score * (buyer_support.购买货物数量 / total_goods_quantities);
        var before = buyer_need.Score + buyer_support.Score;
        //打开所有的记录
        var rs = new List<Result>();
        rs.AddRange(buyer_need.results);
        rs.AddRange(buyer_support.results);
        //Result还原为sellers
        var sellers = new List<Seller>();
        var sellers2 = new List<Seller>();
        foreach (var r in rs)
        {
            sellers.Add(new Seller(r));
            sellers2.Add(new Seller(r));
        }

        var buyer_need_Clone = buyer_need.Clone();
        var buyer_support_Clone = buyer_support.Clone();
        buyer_need_Clone.results.Clear();
        buyer_support_Clone.results.Clear();
        buyer_need_Clone.已分配货物数量 = 0;
        buyer_support_Clone.已分配货物数量 = 0;
        //重新排
        buyer_support_Clone.results = PreAssign.AssignItem(buyer_support_Clone, sellers);
        sellers = sellers.Where(x => !x.是否分配完毕).ToList();
        buyer_need_Clone.results = PreAssign.AssignItem(buyer_need_Clone, sellers);
        //var after_1 = buyer_need_Clone.Score * (buyer_need.购买货物数量 / total_goods_quantities) +
        //              buyer_support_Clone.Score * (buyer_support.购买货物数量 / total_goods_quantities);
        var after_1 = buyer_need_Clone.Score + buyer_support_Clone.Score;


        var buyer_need_Clone2 = buyer_need.Clone();
        var buyer_support_Clone2 = buyer_support.Clone();
        buyer_need_Clone2.results.Clear();
        buyer_support_Clone2.results.Clear();
        buyer_need_Clone2.已分配货物数量 = 0;
        buyer_support_Clone2.已分配货物数量 = 0;
        //重新排
        buyer_need_Clone2.results = PreAssign.AssignItem(buyer_need_Clone2, sellers2);
        sellers2 = sellers2.Where(x => !x.是否分配完毕).ToList();
        buyer_support_Clone2.results = PreAssign.AssignItem(buyer_support_Clone2, sellers2);
        //var after_2 = buyer_need_Clone2.Score * (buyer_need.购买货物数量 / total_goods_quantities) +
        //              buyer_support_Clone2.Score * (buyer_support.购买货物数量 / total_goods_quantities);
        var after_2 = buyer_need_Clone2.Score + buyer_support_Clone2.Score;


        //保持满足和不满足状态
        var Ok1 = true;
        if (buyer_need.IsLockFirstHope && !buyer_need_Clone.IsFirstHopeSatisfy) Ok1 = false;
        if (buyer_support.IsLockFirstHope && !buyer_support_Clone.IsFirstHopeSatisfy) Ok1 = false;
        if (!buyer_need.IsLockFirstHope && buyer_need_Clone.IsContainFirstSatify) Ok1 = false;
        if (!buyer_support.IsLockFirstHope && buyer_support_Clone.IsContainFirstSatify) Ok1 = false;


        var Ok2 = true;
        if (buyer_need.IsLockFirstHope && !buyer_need_Clone2.IsFirstHopeSatisfy) Ok2 = false;
        if (buyer_support.IsLockFirstHope && !buyer_support_Clone2.IsFirstHopeSatisfy) Ok2 = false;
        if (!buyer_need.IsLockFirstHope && buyer_need_Clone2.IsContainFirstSatify) Ok2 = false;
        if (!buyer_support.IsLockFirstHope && buyer_support_Clone2.IsContainFirstSatify) Ok2 = false;

        if (Ok1 && !Ok2)
        {
            double diff = after_1 - before;
            if (diff > 0)
            {
                buyer_need.IsOptiomized = true;
                buyer_support.IsOptiomized = true;
                buyer_need.results = buyer_need_Clone.results;
                buyer_support.results = buyer_support_Clone.results;
                return (int)diff;
            }
        }

        if (!Ok1 && Ok2)
        {
            double diff = after_2 - before;
            if (diff > 0)
            {
                buyer_need.IsOptiomized = true;
                buyer_support.IsOptiomized = true;
                buyer_need.results = buyer_need_Clone2.results;
                buyer_support.results = buyer_support_Clone2.results;
                return (int)diff;
            }
        }

        if (Ok1 && Ok2)
        {
            double diff1 = after_1 - before;
            double diff2 = after_2 - before;
            if (diff1 > diff2)
            {
                if (diff1 > 0)
                {
                    buyer_need.IsOptiomized = true;
                    buyer_support.IsOptiomized = true;
                    buyer_need.results = buyer_need_Clone.results;
                    buyer_support.results = buyer_support_Clone.results;
                    return (int)diff1;
                }
            }
            else
            {
                if (diff2 > 0)
                {
                    buyer_need.IsOptiomized = true;
                    buyer_support.IsOptiomized = true;
                    buyer_need.results = buyer_need_Clone2.results;
                    buyer_support.results = buyer_support_Clone2.results;
                    return (int)diff2;
                }
            }
        }
        return 0;
    }
    #endregion

    private static int IsDetailResult(Buyer buyer_need, Buyer buyer_support)
    {
        //单一仓库  无第一意向 有第一意向，不完美 
        //如果该明细能够满足以下条件则进行交货
        //0.意向得分不满的，多少可以拿点意向分
        //1.防止交换之后，原来无法满意任何意向的记录，满足了第一意向，造成约束违反
        //2.交换之后，意向顺序需要重置
        //3.交换之后，双方总体分数增加（可能造成仓库问题）
        //9.通过IsLockFirstHope判断是否可能违反约束
        for (int i = 0; i < buyer_support.results.Count; i++)
        {
            var r = buyer_support.results[i];
            if (Goods.GoodsDict[r.货物编号].GetHopeScore(buyer_need) == buyer_need.TotalHopeScore)
            {
                var before_buyer_support_分配货物数量 = buyer_support.results.Sum(x => x.分配货物数量);
                var before_buyer_need_分配货物数量 = buyer_need.results.Sum(x => x.分配货物数量);

                var new_rs = Exchange(r, buyer_need);
                buyer_support.results.RemoveAt(i);
                buyer_support.results.AddRange(new_rs);

                //2.交换之后，意向顺序需要重置
                foreach (var r_s in buyer_support.results)
                {
                    //供给方为无意向买家
                    r_s.对应意向顺序 = "0";
                    r_s.买方客户 = buyer_support.买方客户;
                }
                foreach (var r_n in buyer_need.results)
                {
                    r_n.对应意向顺序 = Result.GetHope(buyer_need, Goods.GoodsDict[r_n.货物编号]);
                    r_n.买方客户 = buyer_need.买方客户;
                }
                //3.交换之后，双方总体分数增加（可能造成仓库问题）
                var after_buyer_support_分配货物数量 = buyer_support.results.Sum(x => x.分配货物数量);
                var after_buyer_need_分配货物数量 = buyer_need.results.Sum(x => x.分配货物数量);
                if (before_buyer_need_分配货物数量 != after_buyer_need_分配货物数量)
                {
                    throw new System.Exception("before_buyer_need_分配货物数量错误");
                }
                if (before_buyer_support_分配货物数量 != after_buyer_support_分配货物数量)
                {
                    throw new System.Exception("before_buyer_support_分配货物数量错误");
                }
            }
        }
        return 0;
    }

    /// <summary>
    /// 交换
    /// </summary>
    /// <param name="r"></param>
    /// <param name="buyer"></param>
    /// <returns></returns>
    private static List<Result> Exchange(Result r, Buyer buyer)
    {
        //原来提供者的Result将被替换
        var new_support_results = new List<Result>();
        //能够确保 r 的仓库 和 buyer的仓库一致，buyer的仓库也是一致的
        //R整体和Buyer的关系如下：
        if (r.分配货物数量 == buyer.购买货物数量)
        {
            new_support_results = buyer.results;

            buyer.results = new List<Result>();
            buyer.results.Add(r);

            return new_support_results;
        }
        if (r.分配货物数量 > buyer.购买货物数量)
        {
            //r分拆为两条记录
            var raw_quantity = r.分配货物数量;
            var r_add = r.Clone();
            r_add.分配货物数量 = raw_quantity - buyer.购买货物数量;
            r.分配货物数量 = buyer.购买货物数量;
            new_support_results = buyer.results;
            new_support_results.Add(r_add);
            buyer.results = new List<Result>();
            buyer.results.Add(r);

            return new_support_results;
        }

        if (r.分配货物数量 < buyer.购买货物数量)
        {
            var new_buyer_result = new List<Result>();
            var quantity = 0;
            var isSatisfyQuantity = false;
            //从buyer里面拿出等量的记录，可能牵涉到数据的拆分
            foreach (var r_x in buyer.results)
            {
                if (isSatisfyQuantity)
                {
                    new_buyer_result.Add(r_x);
                    continue;
                }
                //取出数据，看一下加上去之后是否超过
                var new_quantity = quantity + r_x.分配货物数量;
                if (new_quantity <= r.分配货物数量)
                {
                    //没有超过
                    new_support_results.Add(r_x);
                    quantity += r_x.分配货物数量;
                }
                else
                {
                    isSatisfyQuantity = true;
                    //超过的话，又有两种可能性
                    if (new_quantity - r.分配货物数量 == r_x.分配货物数量)
                    {
                        new_buyer_result.Add(r_x);
                    }
                    else
                    {
                        //将需求方的R_X进行拆分
                        //R1:整体移动到供给方的量 - 供给方实际需求的量 = 留在需求方的量
                        var r_1 = r_x.Clone();
                        r_1.分配货物数量 = new_quantity - r.分配货物数量;
                        new_buyer_result.Add(r_1);
                        //R2:需求方的原始量 - 留在需求方的量 = 移动到供给方的量
                        var r_2 = r_x.Clone();
                        r_2.分配货物数量 = r_x.分配货物数量 - r_1.分配货物数量;
                        new_support_results.Add(r_2);
                    }
                }
            }
            new_buyer_result.Add(r);
            buyer.results = new_buyer_result;
            return new_support_results;
        }
        return new_support_results;
    }

}