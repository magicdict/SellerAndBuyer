using System;
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
                //Summary.Run(path, "SR_ReHope.csv");
                Optiomize.ReAssignFirstHope(path, "CF_Inter.csv","CF");
                //Optiomize.ReAssignFirstHope(path, "SR_Inter.csv","SR");
                //Optiomize.OptiomizeInteractive(path, "result_CF_99.csv","CF");
                //Result.CompressResultFile(path + "SR_ReHope.csv");
                return;
            }
            //按照品种进行分组
            var strategylist = new int[] { 1, 2 };
            var kblist = new string[] { "CF" };

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
                    System.Console.WriteLine("具有第一意向的客户数：" + buyers_Breed.Count(x => x.第一意向.hopeType != enmHope.无));
                    System.Console.WriteLine("具有第二意向的客户数：" + buyers_Breed.Count(x => x.第二意向.hopeType != enmHope.无));
                    System.Console.WriteLine("具有第三意向的客户数：" + buyers_Breed.Count(x => x.第三意向.hopeType != enmHope.无));
                    System.Console.WriteLine("具有第四意向的客户数：" + buyers_Breed.Count(x => x.第四意向.hopeType != enmHope.无));
                    System.Console.WriteLine("具有第五意向的客户数：" + buyers_Breed.Count(x => x.第五意向.hopeType != enmHope.无));
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
                results.AddRange(PreAssign.AssignFirstHope_Gap(sellers_remain, buyers_remain));
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
                results.AddRange(PreAssign.AssignFirstHope(sellers_remain, buyers_remain, strategy));
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
                results.AddRange(PreAssign.AssignOthers(sellers_remain, buyers_remain));
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
    }
}
