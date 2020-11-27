﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace src
{
    class Program
    {
        static void Main(string[] args)
        {

            var path = @"F:\基于买方意向的货物撮合交易\data\";
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                path = "";
            }

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            var sellers = Seller.ReadSellerFile(path + "seller.csv");
            var buyers = Buyer.ReadBuyerFile(path + "buyer.csv");
            if (System.IO.File.Exists(path + "result.csv")) System.IO.File.Delete(path + "result.csv");
            //按照品种进行分组

            Parallel.ForEach(sellers.Select(x => x.品种).Distinct(), breed =>
            {
                var sellers_Breed = sellers.Where(x => x.品种 == breed).ToList();
                var buyers_Breed = buyers.Where(x => x.品种 == breed).ToList();
                System.Console.WriteLine("品种：" + breed);
                System.Console.WriteLine("卖家数：" + sellers_Breed.Count);
                System.Console.WriteLine("买家数：" + buyers_Breed.Count);
                List<Result> results = Assign(sellers_Breed, buyers_Breed);
                results.Sort((x, y) => { return (x.买方客户 + x.卖方客户).CompareTo(y.买方客户 + y.卖方客户); });

                int hope_score = results.Sum(x => x.hope_score);
                //按照卖方进行GroupBy，然后Distinct仓库号
                var t = results.GroupBy(x => x.买方客户).Select(y => new { 品种 = y.First().品种, 仓库数 = y.Select(z => z.仓库).Distinct().Count() });
                int Diary_score = t.Sum(x =>
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
                System.Console.WriteLine("意向分数：" + hope_score);
                System.Console.WriteLine("记录分数：" + Diary_score);
                System.Console.WriteLine("总体分数：" + score);
                int score_stardard = buyers_Breed.Count * 100;
                System.Console.WriteLine("得分率：" + (score * 100 / score_stardard) + "%");
                System.Console.WriteLine("总体贸易分单数：" + results.Count);
                Result.AppendToCSV(path + "result.csv", results);
            });
        }

        /// <summary>
        /// 初始分配
        /// </summary>
        /// <param name="sellers"></param>
        /// <param name="buyers"></param>
        static List<Result> Assign(List<Seller> sellers, List<Buyer> buyers)
        {
            System.Console.WriteLine("卖家所有货物数：" + sellers.Sum(x => x.货物数量));
            System.Console.WriteLine("买家所有货物数：" + buyers.Sum(x => x.购买货物数量));
            var results = new List<Result>();
            for (int i = 1; i < 7; i++)
            {
                var sellers_remain = sellers.Where(x => !x.是否分配完毕).ToList();
                var buyers_remain = buyers.Where(x => !x.是否分配完毕).ToList();
                if (i != 6)
                {
                    results.AddRange(AssignWithHope(sellers_remain, buyers_remain, i));
                }
                else
                {
                    foreach (var buyer in buyers_remain)
                    {
                        sellers_remain = sellers.Where(x => !x.是否分配完毕).ToList();
                        results.AddRange(AssignItem(buyer, sellers_remain));
                    }
                }
            }

            //最后的确认
            if (sellers.Count(x => !x.是否分配完毕) != 0)
            {
                System.Console.WriteLine("卖家有剩余（人数）:" + sellers.Count(x => !x.是否分配完毕));
                System.Console.WriteLine("卖家有剩余（货物数）:" + sellers.Sum(x => x.剩余货物数量));
            }
            if (buyers.Count(x => !x.是否分配完毕) != 0)
            {
                System.Console.WriteLine("买家有剩余（人数）:" + buyers.Count(x => !x.是否分配完毕));
                System.Console.WriteLine("买家有剩余（货物数）:" + buyers.Sum(x => x.剩余货物数量));
            }
            return results;
        }

        static List<Result> AssignWithHope(List<Seller> sellers, List<Buyer> buyers, int hope_order)
        {

            //按照第一意向 + 值 进行分组
            IEnumerable<IGrouping<(enmHope, string), Buyer>> hope_group = null;
            switch (hope_order)
            {
                case 1:
                    hope_group = buyers.GroupBy(x => x.第一意向);
                    System.Console.WriteLine("第一意向 类别数：" + hope_group.Count());
                    break;
                case 2:
                    hope_group = buyers.GroupBy(x => x.第二意向);
                    System.Console.WriteLine("第二意向 类别数：" + hope_group.Count());
                    break;
                case 3:
                    hope_group = buyers.GroupBy(x => x.第三意向);
                    System.Console.WriteLine("第三意向 类别数：" + hope_group.Count());
                    break;
                case 4:
                    hope_group = buyers.GroupBy(x => x.第四意向);
                    System.Console.WriteLine("第四意向 类别数：" + hope_group.Count());
                    break;
                case 5:
                    hope_group = buyers.GroupBy(x => x.第五意向);
                    System.Console.WriteLine("第五意向 类别数：" + hope_group.Count());
                    break;
                default:
                    break;
            }

            //对于意向进行分组
            IEnumerable<IGrouping<(enmHope, string), Buyer>> 产地_hope_group = hope_group.Where(x => x.Key.Item1 == enmHope.产地);
            IEnumerable<IGrouping<(enmHope, string), Buyer>> 仓库_hope_group = hope_group.Where(x => x.Key.Item1 == enmHope.仓库);
            IEnumerable<IGrouping<(enmHope, string), Buyer>> 品牌_hope_group = hope_group.Where(x => x.Key.Item1 == enmHope.品牌);
            IEnumerable<IGrouping<(enmHope, string), Buyer>> 年度_hope_group = hope_group.Where(x => x.Key.Item1 == enmHope.年度);
            IEnumerable<IGrouping<(enmHope, string), Buyer>> 等级_hope_group = hope_group.Where(x => x.Key.Item1 == enmHope.等级);
            IEnumerable<IGrouping<(enmHope, string), Buyer>> 类别_hope_group = hope_group.Where(x => x.Key.Item1 == enmHope.类别);

            System.Console.WriteLine("产地_hope_group:" + 产地_hope_group.Count());
            System.Console.WriteLine("仓库_hope_group:" + 仓库_hope_group.Count());
            System.Console.WriteLine("品牌_hope_group:" + 品牌_hope_group.Count());
            System.Console.WriteLine("年度_hope_group:" + 年度_hope_group.Count());
            System.Console.WriteLine("等级_hope_group:" + 等级_hope_group.Count());
            System.Console.WriteLine("类别_hope_group:" + 类别_hope_group.Count());

            var classfied_hope_group = new List<IEnumerable<IGrouping<(enmHope, string), Buyer>>>();
            classfied_hope_group.Add(等级_hope_group);
            classfied_hope_group.Add(类别_hope_group);
            classfied_hope_group.Add(品牌_hope_group);
            classfied_hope_group.Add(年度_hope_group);
            classfied_hope_group.Add(产地_hope_group);
            classfied_hope_group.Sort((x, y) => y.Count().CompareTo(x.Count()));
            //仓库最优先
            classfied_hope_group.Insert(0, 仓库_hope_group);
            var results = new ConcurrentBag<Result>();
            foreach (var hopegrp in classfied_hope_group)
            {
                Parallel.ForEach(hopegrp, grp =>
                    {
                        System.Console.WriteLine("意向 " + grp.Key.Item1 + ":" + grp.Key.Item2 + " (" + grp.Count() + ")");
                        var buyers_grp = grp.ToList();
                        //按照平均持仓时间降序排列，保证时间长的优先匹配
                        buyers_grp.Sort((x, y) => { return y.平均持仓时间.CompareTo(x.平均持仓时间); });
                        var seller_matchhope = sellers.Where(x => x.IsMatchHope(grp.Key));
                        foreach (var buyer in buyers_grp)
                        {
                            //Seller选择 有货物的
                            var sellers_remain = seller_matchhope.Where(x => !x.是否分配完毕).ToList();
                            //如果没有的话，按照其他意愿来分配,这个第一意向组不用再做了
                            if (sellers_remain.Count == 0) break;
                            //如果有的话，按照顺序
                            foreach (var r in AssignItem(buyer, sellers_remain))
                            {
                                results.Add(r);
                            }
                        }
                        System.Console.WriteLine("意向 " + grp.Key.Item1 + ":" + grp.Key.Item2 + " (Complete)");
                    });
            }
            return results.ToList();
        }
        static List<Result> AssignItem(Buyer buyer, List<Seller> sellers_remain)
        {
            var rs = new List<Result>();
            //按照库存排序
            sellers_remain.Sort((x, y) =>
            {
                if (x.仓库 != y.仓库)
                {
                    return x.仓库.CompareTo(y.仓库);
                }
                else
                {
                    return y.剩余货物数量 - x.剩余货物数量;
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
                    r.hope_score = Utility.GetHopeScore(buyer, seller) * quantity / buyer.购买货物数量;
                    rs.Add(r);
                }
                else
                {
                    //买家的需求小于等于卖家的库存
                    quantity = buyer.剩余货物数量;
                    seller.已分配货物数量 += quantity;
                    buyer.已分配货物数量 += quantity;
                    r.分配货物数量 = quantity;
                    r.hope_score = Utility.GetHopeScore(buyer, seller) * quantity / buyer.购买货物数量;
                    rs.Add(r);
                    break;
                }
            }
            return rs;
        }
    }
}
