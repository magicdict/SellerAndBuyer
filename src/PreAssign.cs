using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
public static class PreAssign
{
    public static List<Result> AssignFirstHope_Gap(List<Seller> sellers, List<Buyer> buyers)
    {
        //按照第一意向 + 值 进行分组
        var hope_group = buyers.GroupBy(x => x.第一意向);
        var gap_hope_group = new List<IGrouping<(enmHope hopeType, string hopeValue), Buyer>>();
        //如果有缺口则先处理
        foreach (var grp in hope_group)
        {
            if (grp.Key.hopeType == enmHope.无) continue;
            if (GetHopeNeedGap(grp, sellers) > 0) { gap_hope_group.Add(grp); }
        }
        var results = new ConcurrentBag<Result>();
        //下面的代码之所以可以并行，实际数据中都是某一个大类的问题
        Parallel.ForEach(gap_hope_group, grp =>
        {
            System.Console.WriteLine("意向 " + grp.Key.hopeType + ":" + grp.Key.hopeValue + " (" + grp.Count() + ")");
            var buyers_grp = grp.ToList();
            //按照平均持仓时间降序排列，保证时间长的优先匹配
            //第一意向的时候，必须以平均持仓时间降序排序！
            buyers_grp.Sort(Buyer.Hope_1st_comparison);
            //由于是并行，所以必须要限制卖家范围，不然会造成同时操作同一卖家的行为
            var seller_matchhope = sellers.Where(x => x.IsMatchHope(grp.Key));
            foreach (var buyer in buyers_grp)
            {
                //Seller选择 有货物的
                var sellers_remain = seller_matchhope.Where(x => !x.是否分配完毕).ToList();
                //如果没有的话，按照其他意愿来分配,这个第一意向组不用再做了
                if (sellers_remain.Count == 0) break;
                //如果有的话，按照顺序
                foreach (var r in AssignItemWithRepo(buyer, sellers_remain))
                {
                    results.Add(r);
                }
            }
            System.Console.WriteLine("意向 " + grp.Key.hopeType + ":" + grp.Key.hopeValue + " (Remain:" + buyers_grp.Count(x => !x.是否分配完毕) + ")");
        });
        System.GC.Collect();
        return results.ToList();
    }

    public static List<Result> AssignFirstHope(List<Seller> sellers, List<Buyer> buyers, int strategy)
    {
        //除去Gap处理阶段结束的任务
        var sellers_remain = sellers.Where(x => !x.是否分配完毕).ToList();
        var buyers_remain = buyers.Where(x => !x.是否分配完毕).ToList();

        //按照第一意向 + 值 进行分组
        var hope_group = buyers_remain.GroupBy(x => x.第一意向);
        //除去没有第一意向和根本无法满足需求的
        hope_group = hope_group.Where(x => x.Key.hopeType != enmHope.无 && GetHopeNeedGap(x, sellers_remain) == 0);

        Goods.RemainDict.Clear();
        var buyer_groups = new List<BuyerGroup>();
        //对于意向进行分组
        foreach (var item in hope_group)
        {
            var buyergroup = new BuyerGroup(item.Key, item.ToList());
            Goods.RemainDict.Add(item.Key, sellers_remain.Where(x => x.IsMatchHope(item.Key)).Sum(x => x.剩余货物数量));
            buyer_groups.Add(buyergroup);
        }
        var results = new List<Result>();
        int assign_cnt = 0;
        while (buyer_groups.Count(x => !x.IsFinished) != 0)
        {
            switch (strategy)
            {
                case 1:
                    buyer_groups.Sort(BuyerGroup.BuyerMinRare);
                    break;
                case 2:
                    buyer_groups.Sort(BuyerGroup.BuyerComboRare);
                    break;
                case 3:
                    buyer_groups.Sort(BuyerGroup.BuyerAvgRare);
                    break;
                case 4:
                    buyer_groups.Sort(BuyerGroup.BuyerAvgRare2);
                    break;
                case 5:
                    buyer_groups.Sort(BuyerGroup.BuyerMinRare);
                    break;
                default:
                    //按照供求比率降序排序,不用实时
                    buyer_groups.Sort(BuyerGroup.Evalute_Best);
                    break;
            }
            //弹出栈顶元素
            var buyer = buyer_groups.First().GetBuyer();
            //寻找最好的卖家
            sellers_remain = sellers_remain.Where(x => x.IsMatchHope(buyer.第一意向)).ToList();
            foreach (var r in AssignItemWithRepo(buyer, sellers_remain))
            {
                results.Add(r);
                foreach (var item in Goods.RemainDict)
                {
                    var seller = Goods.GoodsDict[r.货物编号];
                    if (seller.IsMatchHope(item.Key)) Goods.RemainDict[item.Key] -= r.分配货物数量;
                }
            }
            sellers_remain = sellers.Where(x => !x.是否分配完毕).ToList();
            if (buyer_groups.First().IsFinished)
            {
                //强制更新一下
                System.Console.WriteLine("Finished：" + buyer.第一意向.hopeType + "-" + buyer.第一意向.hopeValue);
                System.Console.WriteLine("RemainBuyerCnt:" + buyer_groups.First().RemainBuyerCnt);
                buyer_groups = buyer_groups.Where(x => !x.IsFinished).ToList();
            }
            assign_cnt++;
            if (assign_cnt % 500 == 0)
            {
                Goods.Update(buyers_remain,sellers_remain); 
                foreach (var b in buyers_remain)
                {
                    b.SetRare();
                }
                System.Console.WriteLine(assign_cnt + "/" + buyers.Count);
            }
        }
        return results.ToList();
    }

    public static List<Result> AssignOthers(List<Seller> sellers, List<Buyer> buyers)
    {
        var results = new ConcurrentBag<Result>();
        var sellers_remain = sellers.Where(x => !x.是否分配完毕).ToList();
        buyers.Sort((x, y) =>
        {
            //意向分多的先分配，购物数少的先分配
            if (x.TotalHopeScore != y.TotalHopeScore)
            {
                return y.TotalHopeScore.CompareTo(x.TotalHopeScore);
            }
            else
            {
                return y.购买货物数量.CompareTo(x.购买货物数量);
            }
        });
        int total_cnt = buyers.Count;
        int process_cnt = 0;
        foreach (var buyer in buyers)
        {
            //Seller选择 有货物的
            sellers_remain = sellers_remain.Where(x => !x.是否分配完毕).ToList();
            //如果有第一意向，为了防止意外，错开第一意向匹配的,避免出现持仓时间的违规
            if (buyer.第一意向.hopeType != enmHope.无)
            {
                sellers_remain = sellers_remain.Where(x => !x.IsMatchHope(buyer.第一意向)).ToList();
            }
            //如果没有的话，按照其他意愿来分配,这个第一意向组不用再做了
            foreach (var r in AssignItemWithRepo(buyer, sellers_remain))
            {
                results.Add(r);
            }
            process_cnt++;
            if (process_cnt % 200 == 0) System.Console.WriteLine("Process:" + process_cnt + "/" + total_cnt);
        }
        System.GC.Collect();
        return results.ToList();
    }

    public static List<Result> AssignItemWithRepo(Buyer buyer, List<Seller> sellers_remain)
    {
        if (buyer.results == null) buyer.results = new List<Result>();
        var repos = sellers_remain.GroupBy(x => x.仓库).ToList();
        var BestScore = 0d;
        var BestRepo = "";
        var BestBuyer = new Buyer();
        Parallel.ForEach(repos, repo =>
        {
            var buyer_Clone = buyer.Clone();
            var seller_Clone = new List<Seller>();
            foreach (var seller in repo.ToList())
            {
                seller_Clone.Add(seller.Copy());
            }
            buyer_Clone.results = AssignItem(buyer_Clone, seller_Clone);
            if (buyer_Clone.Score > BestScore && buyer_Clone.是否分配完毕)
            {
                BestRepo = repo.Key;
                BestScore = buyer_Clone.Score;
                BestBuyer = buyer_Clone;
            }
        });

        var buyer_Clone2 = buyer.Clone();
        var seller_Clone2 = new List<Seller>();
        foreach (var seller in sellers_remain)
        {
            seller_Clone2.Add(seller.Copy());
        }
        buyer_Clone2.results = AssignItem(buyer_Clone2, seller_Clone2);
        //如果一个大的单子，但是只有第一意向时候，第一意向是33，但是分仓库的话极端情况就是40，所以这里可能产生
        //大的单子应该优先获取第一意向的，变成了宁可不分单子，而集中在一个仓库中的情况。造成违反规则！

        //是否有第一意向
        var isRoleOK = true;
        if (buyer.第一意向.hopeType != enmHope.无)
        {
            if (BestBuyer.results != null)
            {
                //标准做法使得结果有第一意向或者第一意向可以满足，仓库别做法必须要保证同样的结果
                if (buyer_Clone2.IsFirstHopeSatisfy && !BestBuyer.IsFirstHopeSatisfy) isRoleOK = false;
                if (buyer_Clone2.IsContainFirstSatify && !BestBuyer.IsContainFirstSatify) isRoleOK = false;
            }
            else
            {
                isRoleOK = false;
            }
        }
        if (!isRoleOK)
        {
            System.Console.WriteLine("Found Err:" + buyer.买方客户);
        }
        if (buyer_Clone2.Score >= BestScore || (!isRoleOK)) return AssignItem(buyer, sellers_remain);
        return AssignItem(buyer, sellers_remain.Where(x => x.仓库 == BestRepo).ToList().ToList());
    }


    public static List<Result> AssignItem(Buyer buyer, List<Seller> sellers_remain)
    {
        var rs = new List<Result>();
        //按照库存排序
        sellers_remain.Sort((x, y) =>
        {
            //排序 仓库 + 满意度
            if (buyer.GetHopeScore(y) != buyer.GetHopeScore(x))
            {
                return buyer.GetHopeScore(y).CompareTo(buyer.GetHopeScore(x));
            }
            else
            {
                return x.仓库.CompareTo(y.仓库);
            }
        });
        foreach (var seller in sellers_remain)
        {
            var r = new Result();
            r.买方客户 = buyer.买方客户;
            r.仓库 = seller.仓库;
            r.卖方客户 = seller.卖方客户;
            r.品种 = seller.品种;
            r.货物编号 = seller.货物编号;
            r.对应意向顺序 = Result.GetHope(buyer, seller);
            int quantity = 0;
            if (buyer.剩余货物数量 > seller.剩余货物数量)
            {
                //买家的需求大于卖家的库存
                quantity = seller.剩余货物数量;
                buyer.已分配货物数量 += quantity;
                seller.已分配货物数量 += quantity;
                r.分配货物数量 = quantity;
                r.hope_score = buyer.GetHopeScore(seller) * quantity / buyer.购买货物数量;
                rs.Add(r);
            }
            else
            {
                //买家的需求小于等于卖家的库存
                quantity = buyer.剩余货物数量;
                seller.已分配货物数量 += quantity;
                buyer.已分配货物数量 += quantity;
                r.分配货物数量 = quantity;
                r.hope_score = buyer.GetHopeScore(seller) * quantity / buyer.购买货物数量;
                rs.Add(r);
                break;
            }
        }
        if (buyer.是否分配完毕) buyer.Seller_Buyer_HopeScoreDic = null;
        return rs;
    }

    /// <summary>
    /// 供求缺口
    /// </summary>
    /// <param name="grps"></param>
    /// <param name="sellers"></param>
    /// <returns></returns>
    public static int GetHopeNeedGap(IGrouping<(enmHope hopeType, string hopeValue), Buyer> grps, List<Seller> sellers)
    {
        if (grps.Count() == 0) return 0;
        int NeedGap = 0;
        int Need = grps.Sum(x => x.剩余货物数量);
        int Support = sellers.Where(x => x.IsMatchHope(grps.Key)).Sum(x => x.剩余货物数量);
        if (Need > Support) NeedGap += (Need - Support);
        return NeedGap;
    }
}