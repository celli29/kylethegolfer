using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Web;
using System.Web.Mvc;
using GolfWorld1.Models;
using Microsoft.Ajax.Utilities;
using Microsoft.AspNet.Identity;
using PagedList;

namespace GolfWorld1.Controllers
{
    public class RoundController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Round
        [Authorize(Roles = "Admin, Regular")]
        public ActionResult Index(string sortOrder, string currentFilter, string searchString, int? page)
        {
            ViewBag.DateSortParam = sortOrder == "starttime" ? "starttime_desc" : "starttime";
            ViewBag.CourseSortParam = sortOrder == "course" ? "course_desc" : "course";
            //ViewBag.TeeSortParam = sortOrder == "tee" ? "tee_desc" : "tee";
            ViewBag.ScoreSortParam = sortOrder == "score" ? "score_desc" : "score";
            ViewBag.PuttSortParam = sortOrder == "putt" ? "putt_desc" : "putt";
            ViewBag.FairwaySortParam = sortOrder == "fairway" ? "fairway_desc" : "fairway";
            ViewBag.GIRSortParam = sortOrder == "gir" ? "gir_desc" : "gir";

            if (searchString != null)
            {
                page = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewBag.CurrentFilter = searchString;

            //ViewBag.CurrentSort = sortOrder;
            string tempUserID = User.Identity.GetUserId();

            var rounds = from r in db.Rounds where r.UserGUID.ToString() == tempUserID select r;

            if (!String.IsNullOrEmpty(searchString))
            {
                rounds = rounds.Where(s => s.Course.Name.Contains(searchString));
            }

            switch (sortOrder)
            {
                case "starttime":
                    rounds = rounds.OrderBy(x => x.PlayStartTime);
                    break;
                case "starttime_desc":
                    rounds = rounds.OrderByDescending(x => x.PlayStartTime);
                    break;
                case "course":
                    rounds = rounds.OrderBy(x => x.Course.Name);
                    break;
                case "course_desc":
                    rounds = rounds.OrderByDescending(x => x.Course.Name);
                    break;
                case "score":
                    rounds = rounds.OrderBy(x => x.Score);
                    break;
                case "score_desc":
                    rounds = rounds.OrderByDescending(x => x.Score);
                    break;
                default:
                    rounds = rounds.OrderByDescending(x => x.PlayStartTime);
                    break;
            }

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(rounds.ToPagedList(pageNumber, pageSize));
        }

        // GET: Round/ShowScoreCard
        [Authorize(Roles = "Admin, Regular")]
        public ActionResult ShowScoreCard(int? id)
        {
            double Par3AvgScore;
            double Par4AvgScore;
            double Par5AvgScore;

            double Par3AvgScore10;
            double Par4AvgScore10;
            double Par5AvgScore10;

            double eagle3, birdie3, par3, bogie3, dbogie3, tbogie3;
            double eagle4, birdie4, par4, bogie4, dbogie4, tbogie4;
            double eagle5, birdie5, par5, bogie5, dbogie5, tbogie5;
            double eaglet, birdiet, part, bogiet, dbogiet, tbogiet;

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Round round = db.Rounds.Find(id);

            if (round == null)
            {
                return HttpNotFound();
            }

            CalculateParDistribution(round, out eagle3, out birdie3, out par3, out bogie3, out dbogie3, out tbogie3,
                out eagle4, out birdie4, out par4, out bogie4, out dbogie4, out tbogie4,
                out eagle5, out birdie5, out par5, out bogie5, out dbogie5, out tbogie5,
                out eaglet, out birdiet, out part, out bogiet, out dbogiet, out tbogiet);

            ViewBag.E3 = eagle3;
            ViewBag.Bir3 = birdie3;
            ViewBag.P3 = par3;
            ViewBag.Bo3 = bogie3;
            ViewBag.Db3 = dbogie3;
            ViewBag.Tb3 = tbogie3;

            ViewBag.E4 = eagle4;
            ViewBag.Bir4 = birdie4;
            ViewBag.P4 = par4;
            ViewBag.Bo4 = bogie4;
            ViewBag.Db4 = dbogie4;
            ViewBag.Tb4 = tbogie4;

            ViewBag.E5 = eagle5;
            ViewBag.Bir5 = birdie5;
            ViewBag.P5 = par5;
            ViewBag.Bo5 = bogie5;
            ViewBag.Db5 = dbogie5;
            ViewBag.Tb5 = tbogie5;

            ViewBag.ET = eaglet;
            ViewBag.BirT = birdiet;
            ViewBag.PT = part;
            ViewBag.BoT = bogiet;
            ViewBag.DbT = dbogiet;
            ViewBag.TbT = tbogiet;

            // kpkp: need to pass multiple rounds info for the use so we can show the data below the scorecard
            // Calculate Average Par Score for the whole rounds
            string tempUserID = User.Identity.GetUserId();

            //try
            {
                DbConnection connection = db.Database.Connection;
                connection.Open();
                var command = connection.CreateCommand();

                command.CommandText = "spCalcWholeRounds";
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(new SqlParameter("UserID", tempUserID));

                int p3 = 0;
                int s3 = 0;
                int p4 = 0;
                int s4 = 0;
                int p5 = 0;
                int s5 = 0;

                DbDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    p3 += int.Parse(reader["NumPars3"].ToString());
                    s3 += int.Parse(reader["SumScore3"].ToString());
                    p4 += int.Parse(reader["NumPars4"].ToString());
                    s4 += int.Parse(reader["SumScore4"].ToString());
                    p5 += int.Parse(reader["NumPars5"].ToString());
                    s5 += int.Parse(reader["SumScore5"].ToString());
                }

                Par3AvgScore = s3 / (double)p3;
                Par4AvgScore = s4 / (double)p4;
                Par5AvgScore = s5 / (double)p5;

                reader.Close();
                // end of first call SP for whole rounds

                command.Parameters.Clear();

                command.CommandText = "spCalc10Rounds";
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(new SqlParameter("UserID", tempUserID));
                command.Parameters.Add(new SqlParameter("howManyTimes", 10));

                reader = command.ExecuteReader();

                while (reader.Read())
                {
                    p3 += int.Parse(reader["NumPars3"].ToString());
                    s3 += int.Parse(reader["SumScore3"].ToString());
                    p4 += int.Parse(reader["NumPars4"].ToString());
                    s4 += int.Parse(reader["SumScore4"].ToString());
                    p5 += int.Parse(reader["NumPars5"].ToString());
                    s5 += int.Parse(reader["SumScore5"].ToString());
                }

                Par3AvgScore10 = s3 / (double)p3;
                Par4AvgScore10 = s4 / (double)p4;
                Par5AvgScore10 = s5 / (double)p5;


                reader.Close();
                connection.Close();
            }
            //catch (Exception ex)
            {
                // Print error message
            }
            //finally
            {
            }

            ViewBag.Par3AvgScore = Par3AvgScore;
            ViewBag.Par4AvgScore = Par4AvgScore;
            ViewBag.Par5AvgScore = Par5AvgScore;
            ViewBag.Par3AvgScore10 = Par3AvgScore10;
            ViewBag.Par4AvgScore10 = Par4AvgScore10;
            ViewBag.Par5AvgScore10 = Par5AvgScore10;

            return View(round);
        }

        public void CalculateParDistribution(Round round, out double eagle3, out double birdie3, out double par3, out double bogie3, out double dbogie3, out double tbogie3,
            out double eagle4, out double birdie4, out double par4, out double bogie4, out double dbogie4, out double tbogie4,
            out double eagle5, out double birdie5, out double par5, out double bogie5, out double dbogie5, out double tbogie5,
            out double eaglet, out double birdiet, out double part, out double bogiet, out double dbogiet, out double tbogiet)
        {
            ScoreCard ScoreCardAltScore = round.ScoreCards.FirstOrDefault(a => a.Theme == "AltScore");
            ScoreCard ScoreCardPar = round.ScoreCards.FirstOrDefault(a => a.Theme == "Par");

            int nAlbatross = 0;
            int nEagles = 0;
            int nBirdies = 0;
            int nPars = 0;
            int nBogies = 0;
            int nDbogies = 0;
            int nTbogies = 0;

            int tAlbatross = 0;
            int tEagles = 0;
            int tBirdies = 0;
            int tPars = 0;
            int tBogies = 0;
            int tDbogies = 0;
            int tTbogies = 0;

            double a3, b3, c3, d3, e3, f3;
            double a4, b4, c4, d4, e4, f4;
            double a5, b5, c5, d5, e5, f5;

            a3 = b3 = c3 = d3 = e3 = f3 = 0.0;
            a4 = b4 = c4 = d4 = e4 = f4 = 0.0;
            a5 = b5 = c5 = d5 = e5 = f5 = 0.0;

            int numCertainPar = 0;
            double nominator = 18.0;

            int[] scAltScore = new int[18];
            int[] scPar = new int[18];

            scAltScore = TransposeScoreCard(ScoreCardAltScore);
            scPar = TransposeScoreCard(ScoreCardPar);

            for (int par = 3; par <= 5; par++)
            {
                nAlbatross = nEagles = nBirdies = nPars = nBogies = nDbogies = nTbogies = numCertainPar = 0;

                for (int i = 0; i < 18; i++)
                {
                    if (par == scPar[i])
                    {
                        numCertainPar++;

                        switch (scAltScore[i])
                        {
                            case -3:
                                nAlbatross++;
                                tAlbatross++;
                                break;
                            case -2:
                                nEagles++;
                                tEagles++;
                                break;
                            case -1:
                                nBirdies++;
                                tBirdies++;
                                break;
                            case 0:
                                nPars++;
                                tPars++;
                                break;
                            case 1:
                                nBogies++;
                                tBogies++;
                                break;
                            case 2:
                                nDbogies++;
                                tDbogies++;
                                break;
                            case 3:
                                nTbogies++;
                                tTbogies++;
                                break;
                            default:
                                nTbogies++;
                                tTbogies++;
                                break;
                        }
                    }
                }

                if (par == 3)
                {
                    a3 = (double) nEagles/numCertainPar;
                    b3 = (double) nBirdies/numCertainPar;
                    c3 = (double) nPars/numCertainPar;
                    d3 = (double) nBogies/numCertainPar;
                    e3 = (double) nDbogies/numCertainPar;
                    f3 = (double) nTbogies/numCertainPar;
                }
                else if (par == 4)
                {
                    a4 = (double)nEagles / numCertainPar;
                    b4 = (double)nBirdies / numCertainPar;
                    c4 = (double)nPars / numCertainPar;
                    d4 = (double)nBogies / numCertainPar;
                    e4 = (double)nDbogies / numCertainPar;
                    f4 = (double)nTbogies / numCertainPar; 
                }
                else if (par == 5)
                {
                    a5 = (double)nEagles / numCertainPar;
                    b5 = (double)nBirdies / numCertainPar;
                    c5 = (double)nPars / numCertainPar;
                    d5 = (double)nBogies / numCertainPar;
                    e5 = (double)nDbogies / numCertainPar;
                    f5 = (double)nTbogies / numCertainPar;
                }
            }

            eagle3 = a3;
            birdie3 = b3;
            par3 = c3;
            bogie3 = d3;
            dbogie3 = e3;
            tbogie3 = f3;

            eagle4 = a4;
            birdie4 = b4;
            par4 = c4;
            bogie4 = d4;
            dbogie4 = e4;
            tbogie4 = f4;

            eagle5 = a5;
            birdie5 = b5;
            par5 = c5;
            bogie5 = d5;
            dbogie5 = e5;
            tbogie5 = f5;

            eaglet = tEagles/nominator;
            birdiet = tBirdies / nominator;
            part = tPars / nominator;
            bogiet = tBogies / nominator;
            dbogiet = tDbogies / nominator;
            tbogiet = tTbogies / nominator;
        }

        // kpkp: 5/26/2015 not used currently
        public void CalculateAverageParScore(List<Round> list, out double par3, out double par4, out double par5)
        {
            // scores
            int par3sum = 0;
            int par4sum = 0;
            int par5sum = 0;
            // num pars
            int par3num = 0;
            int par4num = 0;
            int par5num = 0;

            foreach (Round round in list) // tempList, this is first difference
            {
                ScoreCard scPar = new ScoreCard();
                scPar = (from s in db.ScoreCards where s.FRID == round.RID && s.Theme == "Par" select s).FirstOrDefault();

                ScoreCard scScore = new ScoreCard();
                scScore = (from s in db.ScoreCards where s.FRID == round.RID && s.Theme == "Score" select s).FirstOrDefault();

                // TODO: currently working on
                int[] pars = TransposeScoreCard(scPar);
                int[] scores = TransposeScoreCard(scScore);

                for (int i = 0; i < 18; i++)
                {
                    switch (pars[i])
                    {
                        case 3:
                            par3sum += scores[i];
                            par3num++;
                            break;
                        case 4:
                            par4sum += scores[i];
                            par4num++;
                            break;
                        case 5:
                            par5sum += scores[i];
                            par5num++;
                            break;
                        default:
                            break;
                    }
                }
            }

            par3 = par3num != 0 ? par3sum / (double)par3num : 0.0;
            par4 = par4num != 0 ? par4sum / (double)par4num : 0.0;
            par5 = par5num != 0 ? par5sum / (double)par5num : 0.0;
        }

        public int[] TransposeScoreCard(ScoreCard sc)
        {
            int[] array = new int[18];

            if (sc == null)
            {
                return array;
            }

            array[0] = sc.H01 ?? 0;
            array[1] = sc.H02 ?? 0;
            array[2] = sc.H03 ?? 0;
            array[3] = sc.H04 ?? 0;
            array[4] = sc.H05 ?? 0;
            array[5] = sc.H06 ?? 0;

            array[6] = sc.H07 ?? 0;
            array[7] = sc.H08 ?? 0;
            array[8] = sc.H09 ?? 0;
            array[9] = sc.H10 ?? 0;
            array[10] = sc.H11 ?? 0;
            array[11] = sc.H12 ?? 0;

            array[12] = sc.H13 ?? 0;
            array[13] = sc.H14 ?? 0;
            array[14] = sc.H15 ?? 0;
            array[15] = sc.H16 ?? 0;
            array[16] = sc.H17 ?? 0;
            array[17] = sc.H18 ?? 0;

            return array;
        }
    }
}