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
            var IsAdjust = false;

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                path = "";
            }
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            if (IsAdjust)
            {
                Adjust.Run(path);
                return;
            }
            var sellers = Seller.ReadSellerFile(path + "seller.csv");
            var buyers = Buyer.ReadBuyerFile(path + "buyer.csv");
            if (System.IO.File.Exists(path + "result.csv")) System.IO.File.Delete(path + "result.csv");
            //按照品种进行分组
            Parallel.ForEach(sellers.Select(x => x.品种).Distinct(), breed =>
            {
                bool RunFirstStep = false;
                bool RunSecondStep = false;
                bool RunThreeStep = true;
                if (breed == "SR")  //仅对SR测试
                //if (breed == "CF")  //仅对CF测试
                {
                    var sellers_Breed = sellers.Where(x => x.品种 == breed).ToList();
                    var buyers_Breed = buyers.Where(x => x.品种 == breed).ToList();
                    System.Console.WriteLine("品种：" + breed);
                    System.Console.WriteLine("具有第一意向的客户数：" + buyers_Breed.Count(x => x.第一意向.Item1 != enmHope.无));
                    System.Console.WriteLine("具有第二意向的客户数：" + buyers_Breed.Count(x => x.第二意向.Item1 != enmHope.无));
                    System.Console.WriteLine("具有第三意向的客户数：" + buyers_Breed.Count(x => x.第三意向.Item1 != enmHope.无));
                    System.Console.WriteLine("具有第四意向的客户数：" + buyers_Breed.Count(x => x.第四意向.Item1 != enmHope.无));
                    System.Console.WriteLine("具有第五意向的客户数：" + buyers_Breed.Count(x => x.第五意向.Item1 != enmHope.无));
                    System.Console.WriteLine("卖家数：" + sellers_Breed.Count);
                    System.Console.WriteLine("买家数：" + buyers_Breed.Count);
                    List<Result> results = Assign(sellers_Breed, buyers_Breed, RunFirstStep, RunSecondStep, RunThreeStep);
                    Result.Score(results, buyers_Breed);
                    if (RunSecondStep) Result.AppendToCSV(path + "result.csv", results);
                }
            });
        }

        /// <summary>
        /// 初始分配
        /// </summary>
        /// <param name="sellers"></param>
        /// <param name="buyers"></param>
        static List<Result> Assign(List<Seller> sellers, List<Buyer> buyers, bool RunFirstHopeGap, bool RunFirstHope, bool RunOthers)
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
                Result.AppendToCSV(path + "FirstHoepGap_" + strKbn + ".csv", results);
                Buyer.SaveBuyerAssignNumber(path + "FirstHoepGap_Buyer_Assign_" + strKbn + ".csv", buyers);
                Seller.SaveSellerAssignNumber(path + "FirstHoepGap_Seller_Assign_" + strKbn + ".csv", sellers);

                sellers_remain = sellers.Where(x => !x.是否分配完毕).ToList();
                buyers_remain = buyers.Where(x => !x.是否分配完毕).ToList();
                System.Console.WriteLine("第一意向(缺口)分配后买家有剩余（人数）:" + buyers.Count(x => !x.是否分配完毕));
                System.Console.WriteLine("第一意向(缺口)分配后买家有剩余（货物数）:" + buyers.Sum(x => x.剩余货物数量));
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
                results.AddRange(AssignFirstHope(sellers_remain, buyers_remain));
                if (System.IO.File.Exists(path + "FirstHoep_" + strKbn + ".csv")) System.IO.File.Delete(path + "FirstHoep_" + strKbn + ".csv");
                Result.AppendToCSV(path + "FirstHoep_" + strKbn + ".csv", results);
                Buyer.SaveBuyerAssignNumber(path + "FirstHoep_Buyer_Assign_" + strKbn + ".csv", buyers);
                Seller.SaveSellerAssignNumber(path + "FirstHoep_Seller_Assign_" + strKbn + ".csv", sellers);
                sellers_remain = sellers.Where(x => !x.是否分配完毕).ToList();
                buyers_remain = buyers.Where(x => !x.是否分配完毕).ToList();
                System.Console.WriteLine("第一意向分配后买家有剩余（人数）:" + buyers.Count(x => !x.是否分配完毕));
                System.Console.WriteLine("第一意向分配后买家有剩余（货物数）:" + buyers.Sum(x => x.剩余货物数量));
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
            IEnumerable<IGrouping<(enmHope, string), Buyer>> hope_group = buyers.GroupBy(x => x.第一意向);
            //对于意向进行分组
            IEnumerable<IGrouping<(enmHope, string), Buyer>> 产地_hope_group = hope_group.Where(x => x.Key.Item1 == enmHope.产地);
            IEnumerable<IGrouping<(enmHope, string), Buyer>> 仓库_hope_group = hope_group.Where(x => x.Key.Item1 == enmHope.仓库);
            IEnumerable<IGrouping<(enmHope, string), Buyer>> 品牌_hope_group = hope_group.Where(x => x.Key.Item1 == enmHope.品牌);
            IEnumerable<IGrouping<(enmHope, string), Buyer>> 年度_hope_group = hope_group.Where(x => x.Key.Item1 == enmHope.年度);
            IEnumerable<IGrouping<(enmHope, string), Buyer>> 等级_hope_group = hope_group.Where(x => x.Key.Item1 == enmHope.等级);
            IEnumerable<IGrouping<(enmHope, string), Buyer>> 类别_hope_group = hope_group.Where(x => x.Key.Item1 == enmHope.类别);

            System.Console.WriteLine("产地_hope_group:" + 产地_hope_group.Count() + " 缺口：" + GetHopeNeedGap(产地_hope_group, sellers));
            System.Console.WriteLine("仓库_hope_group:" + 仓库_hope_group.Count() + " 缺口：" + GetHopeNeedGap(仓库_hope_group, sellers));
            System.Console.WriteLine("品牌_hope_group:" + 品牌_hope_group.Count() + " 缺口：" + GetHopeNeedGap(品牌_hope_group, sellers));
            System.Console.WriteLine("年度_hope_group:" + 年度_hope_group.Count() + " 缺口：" + GetHopeNeedGap(年度_hope_group, sellers));
            System.Console.WriteLine("等级_hope_group:" + 等级_hope_group.Count() + " 缺口：" + GetHopeNeedGap(等级_hope_group, sellers));
            System.Console.WriteLine("类别_hope_group:" + 类别_hope_group.Count() + " 缺口：" + GetHopeNeedGap(类别_hope_group, sellers));

            var gap_hope_group = new List<IEnumerable<IGrouping<(enmHope, string), Buyer>>>();

            //如果有缺口则先处理
            if (GetHopeNeedGap(仓库_hope_group, sellers) > 0) { gap_hope_group.Add(仓库_hope_group); }
            if (GetHopeNeedGap(等级_hope_group, sellers) > 0) { gap_hope_group.Add(等级_hope_group); }
            if (GetHopeNeedGap(类别_hope_group, sellers) > 0) { gap_hope_group.Add(类别_hope_group); }
            if (GetHopeNeedGap(年度_hope_group, sellers) > 0) { gap_hope_group.Add(年度_hope_group); }
            if (GetHopeNeedGap(产地_hope_group, sellers) > 0) { gap_hope_group.Add(产地_hope_group); }
            if (GetHopeNeedGap(品牌_hope_group, sellers) > 0) { gap_hope_group.Add(品牌_hope_group); }

            var results = new ConcurrentBag<Result>();
            foreach (var hopegrp in gap_hope_group)
            {
                //下面的代码之所以可以并行，因为按照大类的条件进行卖家约束了
                Parallel.ForEach(hopegrp, grp =>
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
            }

            return results.ToList();
        }


        static List<Result> AssignFirstHope(List<Seller> sellers, List<Buyer> buyers)
        {
            //除去Gap处理阶段结束的任务
            sellers = sellers.Where(x => !x.是否分配完毕).ToList();
            buyers = buyers.Where(x => !x.是否分配完毕).ToList();
            //按照第一意向 + 值 进行分组
            IEnumerable<IGrouping<(enmHope, string), Buyer>> hope_group = buyers.GroupBy(x => x.第一意向);
            //对于意向进行分组
            IEnumerable<IGrouping<(enmHope, string), Buyer>> 产地_hope_group = hope_group.Where(x => x.Key.Item1 == enmHope.产地);
            IEnumerable<IGrouping<(enmHope, string), Buyer>> 仓库_hope_group = hope_group.Where(x => x.Key.Item1 == enmHope.仓库);
            IEnumerable<IGrouping<(enmHope, string), Buyer>> 品牌_hope_group = hope_group.Where(x => x.Key.Item1 == enmHope.品牌);
            IEnumerable<IGrouping<(enmHope, string), Buyer>> 年度_hope_group = hope_group.Where(x => x.Key.Item1 == enmHope.年度);
            IEnumerable<IGrouping<(enmHope, string), Buyer>> 等级_hope_group = hope_group.Where(x => x.Key.Item1 == enmHope.等级);
            IEnumerable<IGrouping<(enmHope, string), Buyer>> 类别_hope_group = hope_group.Where(x => x.Key.Item1 == enmHope.类别);

            System.Console.WriteLine("产地_hope_group:" + 产地_hope_group.Count() + " 缺口：" + GetHopeNeedGap(产地_hope_group, sellers));
            System.Console.WriteLine("仓库_hope_group:" + 仓库_hope_group.Count() + " 缺口：" + GetHopeNeedGap(仓库_hope_group, sellers));
            System.Console.WriteLine("品牌_hope_group:" + 品牌_hope_group.Count() + " 缺口：" + GetHopeNeedGap(品牌_hope_group, sellers));
            System.Console.WriteLine("年度_hope_group:" + 年度_hope_group.Count() + " 缺口：" + GetHopeNeedGap(年度_hope_group, sellers));
            System.Console.WriteLine("等级_hope_group:" + 等级_hope_group.Count() + " 缺口：" + GetHopeNeedGap(等级_hope_group, sellers));
            System.Console.WriteLine("类别_hope_group:" + 类别_hope_group.Count() + " 缺口：" + GetHopeNeedGap(类别_hope_group, sellers));

            var nopag_hope_group = new List<IEnumerable<IGrouping<(enmHope, string), Buyer>>>();
            if (GetHopeNeedGap(仓库_hope_group, sellers) == 0) { nopag_hope_group.Add(仓库_hope_group); }
            if (GetHopeNeedGap(等级_hope_group, sellers) == 0) { nopag_hope_group.Add(等级_hope_group); }
            if (GetHopeNeedGap(类别_hope_group, sellers) == 0) { nopag_hope_group.Add(类别_hope_group); }
            if (GetHopeNeedGap(年度_hope_group, sellers) == 0) { nopag_hope_group.Add(年度_hope_group); }
            if (GetHopeNeedGap(产地_hope_group, sellers) == 0) { nopag_hope_group.Add(产地_hope_group); }
            if (GetHopeNeedGap(品牌_hope_group, sellers) == 0) { nopag_hope_group.Add(品牌_hope_group); }

            var results = new ConcurrentBag<Result>();
            //上一个阶段，存在缺口的，能分配完成的都分配完成了，有缺口的，这里也不可能有机会填补缺口了
            //所以这些买家放到下一个阶段考虑
            foreach (var hopegrp in nopag_hope_group)
            {
                //下面的代码之所以可以并行，因为按照大类的条件进行卖家约束了
                Parallel.ForEach(hopegrp, grp =>
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
            }




            //各组，按照时间再分组，有缺口的组也参与
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
                if (process_cnt % 50 == 0) System.Console.WriteLine("Process:" + process_cnt + "/" + total_cnt);
            }
            System.GC.Collect();
            return results.ToList();
        }
        static List<Result> AssignItem(Buyer buyer, List<Seller> sellers_remain)
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
        static int GetHopeNeedGap(IEnumerable<IGrouping<(enmHope, string), Buyer>> grps, List<Seller> sellers)
        {
            if (grps.Count() == 0) return 0;
            int NeedGap = 0;
            foreach (var item in grps)
            {
                int Need = item.Sum(x => x.剩余货物数量);
                int Support = sellers.Where(x => x.IsMatchHope(item.Key)).Sum(x => x.剩余货物数量);
                if (Need > Support) NeedGap += (Need - Support);
            }
            return NeedGap;
        }
    }
}
