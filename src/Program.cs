﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace src
{
    class Program
    {
        static string path = @"F:\基于买方意向的货物撮合交易\data\";
        static void Main(string[] args)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                path = "";
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                path = "/Users/hu/Downloads/SellerAndBuyer-master/";
            }
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            var IsAdjust = true;
            if (IsAdjust)
            {
                Adjust.Optiomize(path, "result_SR_99.csv","SR");
                //Adjust.Optiomize(path, "result_CF_99.csv");
                return;
            }
            //按照品种进行分组
            var strategylist = new int[] { 99 };
            var kblist = new string[] { "CF", "SR" };

            foreach (var strategy in strategylist)
            {
                foreach (var strKb in kblist)
                {
                    bool RunFirstStep = true;
                    bool RunSecondStep = true;
                    bool RunThreeStep = true;
                    var sellers = Seller.ReadSellerFile(path + "seller.csv");
                    var buyers = Buyer.ReadBuyerFile(path + "buyer.csv");
                    var sellers_Breed = sellers.Where(x => x.品种 == strKb).ToList();
                    var buyers_Breed = buyers.Where(x => x.品种 == strKb).ToList();
                    System.Console.WriteLine("品种：" + strKb);
                    System.Console.WriteLine("具有第一意向的客户数：" + buyers_Breed.Count(x => x.第一意向.Item1 != enmHope.无));
                    System.Console.WriteLine("具有第二意向的客户数：" + buyers_Breed.Count(x => x.第二意向.Item1 != enmHope.无));
                    System.Console.WriteLine("具有第三意向的客户数：" + buyers_Breed.Count(x => x.第三意向.Item1 != enmHope.无));
                    System.Console.WriteLine("具有第四意向的客户数：" + buyers_Breed.Count(x => x.第四意向.Item1 != enmHope.无));
                    System.Console.WriteLine("具有第五意向的客户数：" + buyers_Breed.Count(x => x.第五意向.Item1 != enmHope.无));
                    System.Console.WriteLine("卖家数：" + sellers_Breed.Count);
                    System.Console.WriteLine("买家数：" + buyers_Breed.Count);
                    List<Result> results = Assign(sellers_Breed, buyers_Breed, RunFirstStep, RunSecondStep, RunThreeStep, strategy);
                    System.Console.WriteLine("======策略号:" + strategy);
                    System.Console.WriteLine("======区分:" + strKb);
                    Result.Score(results, buyers_Breed);
                    if (RunThreeStep) Result.WriteToCSV(path + "result_" + strKb + "_" + strategy + ".csv", results);
                    System.GC.Collect();
                }
            }
        }

        static void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Console.WriteLine("MyHandler caught : " + e.Message);
            Console.WriteLine("Runtime terminating: {0}", args.IsTerminating);
        }

        /// <summary>
        /// 初始分配
        /// </summary>
        /// <param name="sellers"></param>
        /// <param name="buyers"></param>
        static List<Result> Assign(List<Seller> sellers, List<Buyer> buyers,
            bool RunFirstHopeGap, bool RunFirstHope, bool RunOthers, int strategy)
        {
            var strKbn = buyers.First().品种;
            System.Console.WriteLine("卖家所有货物数：" + sellers.Sum(x => x.货物数量));
            System.Console.WriteLine("买家所有货物数：" + buyers.Sum(x => x.购买货物数量));
            var results = new List<Result>();
            var sellers_remain = sellers;
            var buyers_remain = buyers;

            if (RunFirstHopeGap)
            {
                results.AddRange(AssignFirstHope_Gap(sellers_remain, buyers_remain));
                if (System.IO.File.Exists(path + "FirstHoepGap_" + strKbn + ".csv")) System.IO.File.Delete(path + "FirstHoepGap_" + strKbn + ".csv");
                Result.WriteToCSV(path + "FirstHoepGap_" + strKbn + ".csv", results);
                Buyer.SaveBuyerAssignNumber(path + "FirstHoepGap_Buyer_Assign_" + strKbn + ".csv", buyers);
                Seller.SaveSellerAssignNumber(path + "FirstHoepGap_Seller_Assign_" + strKbn + ".csv", sellers);

                sellers_remain = sellers.Where(x => !x.是否分配完毕).ToList();
                buyers_remain = buyers.Where(x => !x.是否分配完毕).ToList();
                System.Console.WriteLine(strKbn + " 第一意向(缺口)分配后买家有剩余（人数）:" + buyers.Count(x => !x.是否分配完毕));
                System.Console.WriteLine(strKbn + " 第一意向(缺口)分配后买家有剩余（货物数）:" + buyers.Sum(x => x.剩余货物数量));
            }
            else
            {
                //恢复数据
                var buyer_assign = Buyer.LoadBuyerAssignNumber(path + "FirstHoepGap_Buyer_Assign_" + strKbn + ".csv");
                var seller_assign = Seller.LoadSellerAssignNumber(path + "FirstHoepGap_Seller_Assign_" + strKbn + ".csv");
                Parallel.ForEach(buyer_assign, buyer =>
                {
                    buyers.Find(x => x.买方客户 == buyer.买方客户).已分配货物数量 = buyer.已分配货物数量;
                });
                Parallel.ForEach(seller_assign, seller =>
                {
                    //有重复数据，相同货号，但是货物数量不同....不知道为什么不合并
                    sellers.Find(x => x.卖方客户 == seller.卖方客户 &&
                                    x.货物数量 == seller.货物数量 &&
                                    x.货物编号 == seller.货物编号).已分配货物数量 = seller.已分配货物数量;
                });
                //恢复明细记录
                results = Result.ReadFromCSV(path + "FirstHoepGap_" + strKbn + ".csv");
                Parallel.ForEach(results, r =>
                {
                    int m = (buyers_remain.Find(x => x.买方客户 == r.买方客户).购买货物数量);
                    r.hope_score = Utility.ConvertHopeStr2Score(r.对应意向顺序, strKbn) * r.分配货物数量 / m;
                });
            }

            if (RunFirstHope)
            {
                sellers_remain = sellers.Where(x => !x.是否分配完毕).ToList();
                buyers_remain = buyers.Where(x => !x.是否分配完毕).ToList();
                results.AddRange(AssignFirstHope(sellers_remain, buyers_remain, strategy));
                if (System.IO.File.Exists(path + "FirstHoep_" + strKbn + ".csv")) System.IO.File.Delete(path + "FirstHoep_" + strKbn + ".csv");
                Result.WriteToCSV(path + "FirstHoep_" + strKbn + ".csv", results);
                Buyer.SaveBuyerAssignNumber(path + "FirstHoep_Buyer_Assign_" + strKbn + ".csv", buyers);
                Seller.SaveSellerAssignNumber(path + "FirstHoep_Seller_Assign_" + strKbn + ".csv", sellers);
                sellers_remain = sellers.Where(x => !x.是否分配完毕).ToList();
                buyers_remain = buyers.Where(x => !x.是否分配完毕).ToList();
                System.Console.WriteLine(strKbn + " 第一意向分配后买家有剩余（人数）:" + buyers.Count(x => !x.是否分配完毕));
                System.Console.WriteLine(strKbn + " 第一意向分配后买家有剩余（货物数）:" + buyers.Sum(x => x.剩余货物数量));
            }
            else
            {
                //恢复数据
                var buyer_assign = Buyer.LoadBuyerAssignNumber(path + "FirstHoep_Buyer_Assign_" + strKbn + ".csv");
                var seller_assign = Seller.LoadSellerAssignNumber(path + "FirstHoep_Seller_Assign_" + strKbn + ".csv");
                Parallel.ForEach(buyer_assign, buyer =>
                {
                    buyers.Find(x => x.买方客户 == buyer.买方客户).已分配货物数量 = buyer.已分配货物数量;
                });
                Parallel.ForEach(seller_assign, seller =>
                {
                    //有重复数据，相同货号，但是货物数量不同....不知道为什么不合并
                    sellers.Find(x => x.卖方客户 == seller.卖方客户 &&
                                    x.货物数量 == seller.货物数量 &&
                                    x.货物编号 == seller.货物编号).已分配货物数量 = seller.已分配货物数量;
                });
                //恢复明细记录
                results = Result.ReadFromCSV(path + "FirstHoep_" + strKbn + ".csv");
                Parallel.ForEach(results, r =>
                {
                    int m = (buyers_remain.Find(x => x.买方客户 == r.买方客户).购买货物数量);
                    r.hope_score = Utility.ConvertHopeStr2Score(r.对应意向顺序, strKbn) * r.分配货物数量 / m;
                });
            }

            if (RunOthers)
            {
                sellers_remain = sellers.Where(x => !x.是否分配完毕).ToList();
                buyers_remain = buyers.Where(x => !x.是否分配完毕).ToList();
                results.AddRange(AssignOthers(sellers_remain, buyers_remain));
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

        static List<Result> AssignFirstHope_Gap(List<Seller> sellers, List<Buyer> buyers)
        {
            //按照第一意向 + 值 进行分组
            var hope_group = buyers.GroupBy(x => x.第一意向);
            var gap_hope_group = new List<IGrouping<(enmHope, string), Buyer>>();
            //如果有缺口则先处理
            foreach (var grp in hope_group)
            {
                if (grp.Key.Item1 == enmHope.无) continue;
                if (GetHopeNeedGap(grp, sellers) > 0) { gap_hope_group.Add(grp); }
            }
            var results = new ConcurrentBag<Result>();
            //下面的代码之所以可以并行，实际数据中都是某一个大类的问题
            Parallel.ForEach(gap_hope_group, grp =>
            {
                System.Console.WriteLine("意向 " + grp.Key.Item1 + ":" + grp.Key.Item2 + " (" + grp.Count() + ")");
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
                    foreach (var r in AssignItem(buyer, sellers_remain))
                    {
                        results.Add(r);
                    }
                }
                System.Console.WriteLine("意向 " + grp.Key.Item1 + ":" + grp.Key.Item2 + " (Remain:" + buyers_grp.Count(x => !x.是否分配完毕) + ")");
            });
            System.GC.Collect();
            return results.ToList();
        }

        static List<Result> AssignFirstHope(List<Seller> sellers, List<Buyer> buyers, int strategy)
        {
            //除去Gap处理阶段结束的任务
            var sellers_remain = sellers.Where(x => !x.是否分配完毕).ToList();
            var buyers_remain = buyers.Where(x => !x.是否分配完毕).ToList();
            //按照第一意向 + 值 进行分组
            var hope_group = buyers_remain.GroupBy(x => x.第一意向);
            //除去没有第一意向和根本无法满足需求的
            hope_group = hope_group.Where(x => x.Key.Item1 != enmHope.无 && GetHopeNeedGap(x, sellers_remain) == 0);
            var buyer_groups = new List<BuyerGroup>();
            //对于意向进行分组
            var RemainDict = new Dictionary<(enmHope, string), int>();
            RemainDict.Clear();
            foreach (var item in hope_group)
            {
                var buyergroup = new BuyerGroup(item.Key, item.ToList());
                buyergroup.RemainDict = RemainDict;
                RemainDict.Add(item.Key, sellers_remain.Where(x => x.IsMatchHope(item.Key)).Sum(x => x.剩余货物数量));
                System.Console.WriteLine(item.Key.Item1 + " " + item.Key.Item2 + ":" + buyergroup.SupportNeedRate + " (" + RemainDict[item.Key] + ")");
                buyer_groups.Add(buyergroup);
            }
            var results = new List<Result>();
            int assign_cnt = 0;
            while (buyer_groups.Count(x => !x.IsFinished) != 0)
            {
                //按照供求比率降序排序,不用实时
                switch (strategy)
                {
                    case 1:
                        BuyerGroup.Sellers_Remain = sellers_remain;
                        buyer_groups.Sort(BuyerGroup.Evalute_1);
                        break;
                    default:
                        buyer_groups.Sort(BuyerGroup.Evalute_Best);
                        break;
                }
                //弹出栈顶元素
                var buyer = buyer_groups.First().GetBuyer();
                //寻找最好的卖家
                sellers_remain = sellers_remain.Where(x => x.IsMatchHope(buyer.第一意向)).ToList();
                foreach (var r in AssignItem(buyer, sellers_remain, RemainDict))
                {
                    results.Add(r);
                }
                sellers_remain = sellers.Where(x => !x.是否分配完毕).ToList();
                if (buyer_groups.First().IsFinished)
                {
                    //强制更新一下
                    System.Console.WriteLine("Finished：" + buyer.第一意向.Item1 + "-" + buyer.第一意向.Item2);
                    System.Console.WriteLine("RemainDict:" + RemainDict[buyer.第一意向]);
                    System.Console.WriteLine("RemainBuyerCnt:" + buyer_groups.First().RemainBuyerCnt);
                    buyer_groups = buyer_groups.Where(x => !x.IsFinished).ToList();
                }
                assign_cnt++;
                if (assign_cnt % 500 == 0)
                {
                    System.Console.WriteLine(assign_cnt + "/" + buyers.Count);
                }
            }
            return results.ToList();
        }

        static List<Result> AssignOthers(List<Seller> sellers, List<Buyer> buyers)
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
                    return x.购买货物数量.CompareTo(y.购买货物数量);
                }
            });
            int total_cnt = buyers.Count;
            int process_cnt = 0;
            foreach (var buyer in buyers)
            {
                //Seller选择 有货物的
                sellers_remain = sellers_remain.Where(x => !x.是否分配完毕).ToList();
                //如果有第一意向，为了防止意外，错开第一意向匹配的,避免出现持仓时间的违规
                if (buyer.第一意向.Item1 != enmHope.无)
                {
                    sellers_remain = sellers_remain.Where(x => !x.IsMatchHope(buyer.第一意向)).ToList();
                }
                //如果没有的话，按照其他意愿来分配,这个第一意向组不用再做了
                foreach (var r in AssignItem(buyer, sellers_remain))
                {
                    results.Add(r);
                }
                process_cnt++;
                if (process_cnt % 200 == 0) System.Console.WriteLine("Process:" + process_cnt + "/" + total_cnt);
            }
            System.GC.Collect();
            return results.ToList();
        }
        static List<Result> AssignItem(Buyer buyer, List<Seller> sellers_remain, Dictionary<(enmHope, string), int> RemainDict = null)
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
                    if (RemainDict != null)
                    {
                        foreach (var item in RemainDict)
                        {
                            if (seller.IsMatchHope(item.Key)) RemainDict[item.Key] -= quantity;
                        }
                    }
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
                    if (RemainDict != null)
                    {
                        foreach (var item in RemainDict)
                        {
                            if (seller.IsMatchHope(item.Key)) RemainDict[item.Key] -= quantity;
                        }
                    }
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
        static int GetHopeNeedGap(IGrouping<(enmHope, string), Buyer> grps, List<Seller> sellers)
        {
            if (grps.Count() == 0) return 0;
            int NeedGap = 0;
            int Need = grps.Sum(x => x.剩余货物数量);
            int Support = sellers.Where(x => x.IsMatchHope(grps.Key)).Sum(x => x.剩余货物数量);
            if (Need > Support) NeedGap += (Need - Support);
            return NeedGap;
        }
    }
}
