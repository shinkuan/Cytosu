using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TaimaniaMaker
{
    /* StoryBoard Format:
             * Ex:
               Sprite,Foreground,Centre,"SB\taikohitcircleoverlay.png",0,197
               _(event),(easing),(starttime),(endtime),(params...)
               _MX,0,339044,340244,440
               _MY,0,339044,340244,-640,400
               _F,0,340244,,1,0
               _S,0,340244,,0.456
               
             * Map Format
             * X,Y,Time,Type,Type2,(WeDontCare),Time2
             * 
             */

    /*
[Events]
//Background and Video events
//Storyboard Layer 0 (Background)
//Storyboard Layer 1 (Fail)
//Storyboard Layer 2 (Pass)
//Storyboard Layer 3 (Foreground)
//Storyboard Layer 4 (Overlay)
Sprite,Overlay,Centre,"SB\BG.png",320,240
M,0,0,999999,320,240
Sprite,Overlay,Centre,"SB\JudgementLine.png",320,240
M,0,0,999999,320,470
Sprite,Overlay,Centre,"SB\Note1.png",320,240
M,0,23436,,245,470
Sprite,Overlay,Centre,"SB\Note2.png",320,240
M,0,23436,,295,470
Sprite,Overlay,Centre,"SB\Note2.png",320,240
M,0,23436,,345,470
Sprite,Overlay,Centre,"SB\Note4.png",320,240
M,0,23436,,395,470
//Storyboard Sound Samples
     *
     *
     */
    //Settings:
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {

            int dropTime = 460;
            int judgementPos = 425;
            int judgementSpace = 30;
            int judgeOffset = 0;
            int NoteHeight = 15;

            float pxms;

            bool isMania = false;
            bool judgelineBottom = true;
            int hitObjectIndex = 0;
            int timingPointIndex = 0;
            List<float> timePointOffset = new List<float>();
            List<float> timePointTPB = new List<float>();   //TPB: time(ms) per beat
            string path;
            string songPath;
            string[] mapHitObjects;
            int songLength = 0;
            string thisPath;

            Console.ForegroundColor = ConsoleColor.Cyan;
            //Get Map File Path
            Console.WriteLine("Select Map File (.osu)");
            OpenFileDialog ofd = new OpenFileDialog()
            {
                FileName = "Select a osu file",
                Filter = "Osu files (*.osu)|*.osu",
                Title = "Open Osu Map file"
            };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Got File Path:");
                Console.WriteLine(ofd.FileName);
            }
            path = ofd.FileName;
            //tt
            //Read The File
            mapHitObjects = File.ReadAllLines(@path);
            //StreamReader file = new StreamReader(@path);
            foreach (var line in mapHitObjects)
            {
                if (line.Contains("Mode: 3"))
                {
                    isMania = true;
                }

                if (line.Contains("[TimingPoints]"))
                {
                    timingPointIndex = hitObjectIndex;
                }

                if (line.Contains("[HitObjects]"))
                {
                    break;
                }
                hitObjectIndex++;
            }
            if (!isMania)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("This is not a Mania Map!");
                Console.WriteLine("Press any key to quit.");
                Console.ReadKey();
                Environment.Exit(0);
            }

            string pattern = @"[0-9]+";
            Regex rgx = new Regex(pattern);
            int counter = 0;
            foreach (Match match in rgx.Matches(mapHitObjects[mapHitObjects.Length - 1]))
            {
                counter++;
                if (counter == 3)
                {
                    songLength = Int32.Parse(match.Value);
                }
                if (counter == 6)
                {
                    if (match.Value != "0")
                    {
                        songLength = Int32.Parse(match.Value);
                    }
                }
            }

            string pattern2 = @"[0-9.]+";
            Regex rgx2 = new Regex(pattern2);

            //get time points
            for (int i = timingPointIndex + 1; i < hitObjectIndex; i++)
            {
                MatchCollection matches = rgx2.Matches(mapHitObjects[i]);

                //1: BPM Change, 0: Velocity Change
                if (matches.Count > 2)
                    if (matches[6].Value == "1")
                    {
                        timePointOffset.Add(float.Parse(matches[0].Value));
                        timePointTPB.Add(float.Parse(matches[1].Value));
                    }
            }

            string sbPath;
            string regex = @" \[[a-zA-Z0-9_ ']+\]\.osu";
            sbPath = Regex.Replace(path, regex, ".osb").Trim();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Writing file to" + sbPath);
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            using (StreamWriter file = new StreamWriter(@sbPath))
            {
                file.WriteLine("[Events]");
                file.WriteLine("Sprite,Overlay,Centre,\"SB\\Cytus.png\",320,240");
                file.WriteLine(" M,0,-5000," + (songLength + 15000) + ",320,240");
                file.WriteLine("Sprite,Overlay,Centre,\"SB\\BG.png\",320,240");
                file.WriteLine(" M,0,-5000," + (songLength + 15000) + ",320,240");
                file.WriteLine("Sprite,Overlay,Centre,\"SB\\JudgementLine.png\",320,240");
                file.WriteLine(" M,0," + (int)(timePointOffset[0] - timePointTPB[0] * 4) + "," + (int)(timePointOffset[0] - timePointTPB[0] * 2) + ",320," + (480 - judgementSpace) + ",320," + (judgementSpace) + "");
                file.WriteLine(" V,0," + (int)(timePointOffset[0] - timePointTPB[0] * 4) + "," + (int)(timePointOffset[0] - timePointTPB[0] * 2) + ",0,1,1,1");
                file.WriteLine(" M,0," + (int)(timePointOffset[0] - timePointTPB[0] * 2) + "," + (int)(timePointOffset[0]) + ",320," + (judgementSpace) + ",320," + (480 - judgementSpace) + "");

                //SetUp JudgementLine Movement
                for (int i = 0; i < timePointOffset.Count; i++)
                {
                    //NotLastTimePoint
                    if (i + 1 != timePointOffset.Count)
                    {
                        for (float time = timePointOffset[i]; time < timePointOffset[i + 1]; time = (time + timePointTPB[i] * 2))
                        {
                            if (time + timePointTPB[i] * 2 > timePointOffset[i + 1])
                            {
                                if (judgelineBottom)
                                {
                                    file.WriteLine(" M,0," + (int)(time) + "," + (int)timePointOffset[i + 1] + ",320," + (480 - judgementSpace) + ",320," + (480 - judgementSpace - (timePointOffset[i + 1] - time) / timePointTPB[i] / 2 * (480 - judgementSpace * 2)) + "");
                                    judgelineBottom = false;
                                    break;
                                }
                                else
                                {
                                    file.WriteLine(" M,0," + (int)(time) + "," + (int)timePointOffset[i + 1] + ",320," + (judgementSpace) + ",320," + (judgementSpace + (timePointOffset[i + 1] - time) / timePointTPB[i] / 2 * (480 - judgementSpace * 2)) + "");
                                    judgelineBottom = true;
                                    break;
                                }
                            }

                            if (judgelineBottom)
                            {
                                file.WriteLine(" M,0," + (int)(time) + "," + (int)(time + timePointTPB[i] * 2) + ",320," + (480 - judgementSpace) + ",320," + (judgementSpace) + "");
                                judgelineBottom = false;
                            }
                            else
                            {
                                file.WriteLine(" M,0," + (int)(time) + "," + (int)(time + timePointTPB[i] * 2) + ",320," + (judgementSpace) + ",320," + (480 - judgementSpace) + "");
                                judgelineBottom = true;
                            }
                        }
                    }
                    else
                    //lastTimePoint
                    {
                        for (float time = timePointOffset[i]; time < songLength + timePointTPB[i] * 2; time = (time + timePointTPB[i] * 2))
                        {
                            if (time + timePointTPB[i] * 2 > songLength + timePointTPB[i] * 2)
                            {
                                file.WriteLine(" V,0," + (int)(time) + "," + (int)(time + timePointTPB[i] * 2) + ",1,1,0,1");
                            }

                            if (judgelineBottom)
                            {
                                file.WriteLine(" M,0," + (int)(time) + "," + (int)(time + timePointTPB[i] * 2) + ",320," + (480 - judgementSpace) + ",320," + (judgementSpace) + "");
                                judgelineBottom = false;
                            }
                            else
                            {
                                file.WriteLine(" M,0," + (int)(time) + "," + (int)(time + timePointTPB[i] * 2) + ",320," + (judgementSpace) + ",320," + (480 - judgementSpace) + "");
                                judgelineBottom = true;
                            }
                        }

                    }
                    judgelineBottom = true;
                }


                int nowTimePoint = 0;
                if (isMania)
                {
                    for (int i = hitObjectIndex + 1; i < mapHitObjects.Length; i++)
                    {
                        MatchCollection matches = rgx.Matches(mapHitObjects[i]);

                        int myPosision;
                        int myTime = Int32.Parse(matches[2].Value);
                        //get which time point this note belongs to
                        for (int j = nowTimePoint; j < timePointOffset.Count; j++)
                        {
                            if (j + 1 + 1 > timePointOffset.Count)
                            {
                                nowTimePoint = j;
                            }
                            else
                            {
                                if (myTime < timePointOffset[j + 1])
                                {
                                    nowTimePoint = j;
                                    break;
                                }
                            }
                        }

                        float beat = ((myTime - timePointOffset[nowTimePoint]) / timePointTPB[nowTimePoint] + 0.01f) % 4;
                        if (beat < 2)
                        {
                            myPosision = 480 - judgementSpace - (int)((480 - judgementSpace * 2) * beat / 2);
                        }
                        else
                        {
                            myPosision = judgementSpace + (int)((480 - judgementSpace * 2) * (beat - 2) / 2);
                        }

                        int waitTime = (int)timePointTPB[nowTimePoint] * 2;
                        if (timePointTPB[nowTimePoint] > 3000)
                        {
                            waitTime = 6000;
                        }

                        //Note
                        if (matches[5].Value == "0")
                        {

                            if (beat < 2)
                                switch (matches[0].Value)
                                {
                                    case "64":
                                        Console.WriteLine("Debug:N1");
                                        file.WriteLine("Sprite,Overlay,Centre,\"SB\\Note1U.png\",320,240");
                                        file.WriteLine(" M,0," + (int)(myTime - waitTime) + "," + myTime + ",80," + myPosision + ",80," + myPosision);
                                        file.WriteLine(" F,0," + (int)(myTime - waitTime) + "," + myTime + ",0,1");
                                        break;
                                    case "192":
                                        Console.WriteLine("Debug:N2");
                                        file.WriteLine("Sprite,Overlay,Centre,\"SB\\Note2U.png\",320,240");
                                        file.WriteLine(" M,0," + (int)(myTime - waitTime) + "," + myTime + ",240," + myPosision + ",240," + myPosision);
                                        file.WriteLine(" F,0," + (int)(myTime - waitTime) + "," + myTime + ",0,1");
                                        break;
                                    case "320":
                                        Console.WriteLine("Debug:N3");
                                        file.WriteLine("Sprite,Overlay,Centre,\"SB\\Note3U.png\",320,240");
                                        file.WriteLine(" M,0," + (int)(myTime - waitTime) + "," + myTime + ",400," + myPosision + ",400," + myPosision);
                                        file.WriteLine(" F,0," + (int)(myTime - waitTime) + "," + myTime + ",0,1");
                                        break;
                                    case "448":
                                        Console.WriteLine("Debug:N4");
                                        file.WriteLine("Sprite,Overlay,Centre,\"SB\\Note4U.png\",320,240");
                                        file.WriteLine(" M,0," + (int)(myTime - waitTime) + "," + myTime + ",560," + myPosision + ",560," + myPosision);
                                        file.WriteLine(" F,0," + (int)(myTime - waitTime) + "," + myTime + ",0,1");
                                        break;
                                }
                            if (beat >= 2)
                                switch (matches[0].Value)
                                {
                                    case "64":
                                        Console.WriteLine("Debug:N1");
                                        file.WriteLine("Sprite,Overlay,Centre,\"SB\\Note1D.png\",320,240");
                                        file.WriteLine(" M,0," + (int)(myTime - waitTime) + "," + myTime + ",80," + myPosision + ",80," + myPosision);
                                        file.WriteLine(" F,0," + (int)(myTime - waitTime) + "," + myTime + ",0,1");
                                        break;
                                    case "192":
                                        Console.WriteLine("Debug:N2");
                                        file.WriteLine("Sprite,Overlay,Centre,\"SB\\Note2D.png\",320,240");
                                        file.WriteLine(" M,0," + (int)(myTime - waitTime) + "," + myTime + ",240," + myPosision + ",240," + myPosision);
                                        file.WriteLine(" F,0," + (int)(myTime - waitTime) + "," + myTime + ",0,1");
                                        break;
                                    case "320":
                                        Console.WriteLine("Debug:N3");
                                        file.WriteLine("Sprite,Overlay,Centre,\"SB\\Note3D.png\",320,240");
                                        file.WriteLine(" M,0," + (int)(myTime - waitTime) + "," + myTime + ",400," + myPosision + ",400," + myPosision);
                                        file.WriteLine(" F,0," + (int)(myTime - waitTime) + "," + myTime + ",0,1");
                                        break;
                                    case "448":
                                        Console.WriteLine("Debug:N4");
                                        file.WriteLine("Sprite,Overlay,Centre,\"SB\\Note4D.png\",320,240");
                                        file.WriteLine(" M,0," + (int)(myTime - waitTime) + "," + myTime + ",560," + myPosision + ",560," + myPosision);
                                        file.WriteLine(" F,0," + (int)(myTime - waitTime) + "," + myTime + ",0,1");
                                        break;
                                }
                        }
                        else
                        //Slider
                        {
                            //ex: 100ms slider
                            //note height: 15px
                            //RunLength: judgementPos
                            //time: dropTIme
                            //1px/ms = judgementPos/dropTime
                            //slider height: sliderTime * 1px/ms

                            int myEndTime = Int32.Parse(matches[5].Value);
                            float beatLength = (myEndTime - myTime) / timePointTPB[nowTimePoint];
                            float beatEnd = (beat + beatLength) % 4;
                            int beatFullLine = (int)(beatLength - (2 - beat % 2)) / 2;

                            pxms = (480 - judgementSpace * 2) / (timePointTPB[nowTimePoint] * 2);

                            bool isUp = beat < 2;

                            int myEndPosition;
                            if (beatEnd < 2)
                            {
                                myEndPosition = (int)(480 - judgementSpace - ((480 - judgementSpace * 2) * beatEnd / 2));
                            }
                            else
                            {
                                myEndPosition = (int)(judgementSpace + ((480 - judgementSpace * 2) * (beatEnd - 2) / 2));
                            }
                            float endOfFirstLN = timePointOffset[nowTimePoint] - 3;
                            while (endOfFirstLN <= myTime)
                            {
                                endOfFirstLN += timePointTPB[nowTimePoint] * 2;
                            }
                            //endOfFirstLN += timePointTPB[nowTimePoint] * 2;
                            
                            switch (matches[0].Value)
                            {
                                case "64":
                                    Console.WriteLine("Debug:LN1");
                                    if (isUp)
                                    {
                                        file.WriteLine("Sprite,Overlay,TopCentre,\"SB\\Note1U.png\",320,240");
                                        file.WriteLine(" F,0," + (int)(myTime - waitTime) + "," + myTime + ",0,1");

                                        if (beat % 2 + beatLength < 2)
                                        {
                                            //LN that is short enough to be in 1 
                                            file.WriteLine(" M,0," + (int)(myTime - waitTime) + "," + myEndTime + ",80," + myEndPosition + ",80," + myEndPosition);
                                            file.WriteLine(" V,0," + (int)(myTime - waitTime) + "," + myTime + ",1," + (float)((float)(Int32.Parse(matches[5].Value) - Int32.Parse(matches[2].Value)) * pxms / NoteHeight) + "");
                                            file.WriteLine(" V,0," + myTime + "," + myEndTime + ",1," + (float)((float)(Int32.Parse(matches[5].Value) - Int32.Parse(matches[2].Value)) * (float)(pxms / NoteHeight)) + ",1,0");
                                        }
                                        //LN is too Long and need to spilt
                                        else
                                        {
                                            //Head LN
                                            file.WriteLine(" M,0," + (int)(myTime - waitTime) + "," + (int)endOfFirstLN + ",80," + judgementSpace + ",80," + judgementSpace);
                                            file.WriteLine(" V,0," + (int)(myTime - waitTime) + "," + (int)myTime + ",1," + ((2 - beat % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight));
                                            file.WriteLine(" V,0," + (int)(myTime) + "," + (int)endOfFirstLN + ",1," + ((2 - beat % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                            isUp = false;
                                            //Mid LN
                                            while (beatFullLine > 0)
                                            {
                                                if (isUp)
                                                {
                                                    file.WriteLine("Sprite,Overlay,TopCentre,\"SB\\Note1U.png\",320,240");
                                                    file.WriteLine(" F,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN) + ",0,1");
                                                    file.WriteLine(" M,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN + waitTime) + ",80," + (judgementSpace));
                                                    file.WriteLine(" V,0," + (int)(endOfFirstLN - waitTime) + "," + (int)endOfFirstLN + ",1," + ((480 - judgementSpace * 2) / NoteHeight) + "");
                                                    file.WriteLine(" V,0," + (int)(endOfFirstLN) + "," + (int)(endOfFirstLN + waitTime) + ",1," + ((480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                                    endOfFirstLN += waitTime;
                                                    isUp = !isUp;
                                                    beatFullLine--;
                                                }
                                                else
                                                {
                                                    file.WriteLine("Sprite,Overlay,BottomCentre,\"SB\\Note1D.png\",320,240");
                                                    file.WriteLine(" F,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN) + ",0,1");
                                                    file.WriteLine(" M,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN + waitTime) + ",80," + (480 - judgementSpace));
                                                    file.WriteLine(" V,0," + (int)(endOfFirstLN - waitTime) + "," + (int)endOfFirstLN + ",1," + ((480 - judgementSpace * 2) / NoteHeight) + "");
                                                    file.WriteLine(" V,0," + (int)(endOfFirstLN) + "," + (int)(endOfFirstLN + waitTime) + ",1," + ((480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                                    endOfFirstLN += waitTime;
                                                    isUp = !isUp;
                                                    beatFullLine--;
                                                }
                                            }
                                            //End LN
                                            if (isUp)
                                            {
                                                file.WriteLine("Sprite,Overlay,TopCentre,\"SB\\Note1U.png\",320,240");
                                                file.WriteLine(" F,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN) + ",0,1");
                                                file.WriteLine(" M,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(myEndTime) + ",80," + myEndPosition + ",80," + myEndPosition);
                                                file.WriteLine(" V,0," + (int)(endOfFirstLN - waitTime) + "," + (int)endOfFirstLN + ",1," + ((beatEnd % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + "");
                                                file.WriteLine(" V,0," + (int)(endOfFirstLN) + "," + (int)myEndTime + ",1," + ((beatEnd % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                            }
                                            else
                                            {
                                                file.WriteLine("Sprite,Overlay,BottomCentre,\"SB\\Note1D.png\",320,240");
                                                file.WriteLine(" F,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN) + ",0,1");
                                                file.WriteLine(" M,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(myEndTime) + ",80," + (myEndPosition) + ",80," + myEndPosition);
                                                file.WriteLine(" V,0," + (int)(endOfFirstLN - waitTime) + "," + (int)endOfFirstLN + ",1," + ((beatEnd % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + "");
                                                file.WriteLine(" V,0," + (int)(endOfFirstLN) + "," + (int)myEndTime + ",1," + ((beatEnd % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        file.WriteLine("Sprite,Overlay,BottomCentre,\"SB\\Note1D.png\",320,240");
                                        file.WriteLine(" F,0," + (int)(myTime - waitTime) + "," + myTime + ",0,1");

                                        if (beat % 2 + beatLength < 2)
                                        {
                                            //LN that is short enough to be in 1 
                                            file.WriteLine(" M,0," + (int)(myTime - waitTime) + "," + myEndTime + ",80," + myEndPosition + ",80," + myEndPosition);
                                            file.WriteLine(" V,0," + (int)(myTime - waitTime) + "," + myTime + ",1," + (float)((float)(Int32.Parse(matches[5].Value) - Int32.Parse(matches[2].Value)) * pxms / NoteHeight) + "");
                                            file.WriteLine(" V,0," + myTime + "," + myEndTime + ",1," + (float)((float)(Int32.Parse(matches[5].Value) - Int32.Parse(matches[2].Value)) * (float)(pxms / NoteHeight)) + ",1,0");
                                        }
                                        //LN is too Long and need to spilt
                                        else
                                        {
                                            //Head LN
                                            file.WriteLine(" M,0," + (int)(myTime - waitTime) + "," + (int)endOfFirstLN + ",80," + (480-judgementSpace) + ",80," + (480 - judgementSpace));
                                            file.WriteLine(" V,0," + (int)(myTime - waitTime) + "," + (int)myTime + ",1," + ((2 - beat % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight));
                                            file.WriteLine(" V,0," + (int)(myTime) + "," + (int)endOfFirstLN + ",1," + ((2 - beat % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                            isUp = true;
                                            //Mid LN
                                            while (beatFullLine > 0)
                                            {
                                                if (isUp)
                                                {
                                                    file.WriteLine("Sprite,Overlay,TopCentre,\"SB\\Note1U.png\",320,240");
                                                    file.WriteLine(" F,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN) + ",0,1");
                                                    file.WriteLine(" M,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN + waitTime) + ",80," + (judgementSpace));
                                                    file.WriteLine(" V,0," + (int)(endOfFirstLN - waitTime) + "," + (int)endOfFirstLN + ",1," + ((480 - judgementSpace * 2) / NoteHeight) + "");
                                                    file.WriteLine(" V,0," + (int)(endOfFirstLN) + "," + (int)(endOfFirstLN + waitTime) + ",1," + ((480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                                    endOfFirstLN += waitTime;
                                                    isUp = !isUp;
                                                    beatFullLine--;
                                                }
                                                else
                                                {
                                                    file.WriteLine("Sprite,Overlay,BottomCentre,\"SB\\Note1D.png\",320,240");
                                                    file.WriteLine(" F,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN) + ",0,1");
                                                    file.WriteLine(" M,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN + waitTime) + ",80," + (480 - judgementSpace));
                                                    file.WriteLine(" V,0," + (int)(endOfFirstLN - waitTime) + "," + (int)endOfFirstLN + ",1," + ((480 - judgementSpace * 2) / NoteHeight) + "");
                                                    file.WriteLine(" V,0," + (int)(endOfFirstLN) + "," + (int)(endOfFirstLN + waitTime) + ",1," + ((480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                                    endOfFirstLN += waitTime;
                                                    isUp = !isUp;
                                                    beatFullLine--;
                                                }
                                            }
                                            //End LN
                                            if (isUp)
                                            {
                                                file.WriteLine("Sprite,Overlay,TopCentre,\"SB\\Note1U.png\",320,240");
                                                file.WriteLine(" F,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN) + ",0,1");
                                                file.WriteLine(" M,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(myEndTime) + ",80," + myEndPosition + ",80," + myEndPosition);
                                                file.WriteLine(" V,0," + (int)(endOfFirstLN - waitTime) + "," + (int)endOfFirstLN + ",1," + ((beatEnd % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + "");
                                                file.WriteLine(" V,0," + (int)(endOfFirstLN) + "," + (int)myEndTime + ",1," + ((beatEnd % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                            }
                                            else
                                            {
                                                file.WriteLine("Sprite,Overlay,BottomCentre,\"SB\\Note1D.png\",320,240");
                                                file.WriteLine(" F,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN) + ",0,1");
                                                file.WriteLine(" M,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(myEndTime) + ",80," + (myEndPosition) + ",80," + myEndPosition);
                                                file.WriteLine(" V,0," + (int)(endOfFirstLN - waitTime) + "," + (int)endOfFirstLN + ",1," + ((beatEnd % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + "");
                                                file.WriteLine(" V,0," + (int)(endOfFirstLN) + "," + (int)myEndTime + ",1," + ((beatEnd % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                            }
                                        }
                                    }
                                    break;
                                case "192":
                                    if (isUp)
                                    {
                                        file.WriteLine("Sprite,Overlay,TopCentre,\"SB\\Note1U.png\",320,240");
                                        file.WriteLine(" F,0," + (int)(myTime - waitTime) + "," + myTime + ",0,1");

                                        if (beat % 2 + beatLength < 2)
                                        {
                                            //LN that is short enough to be in 1 
                                            file.WriteLine(" M,0," + (int)(myTime - waitTime) + "," + myEndTime + ",240," + myEndPosition + ",240," + myEndPosition);
                                            file.WriteLine(" V,0," + (int)(myTime - waitTime) + "," + myTime + ",1," + (float)((float)(Int32.Parse(matches[5].Value) - Int32.Parse(matches[2].Value)) * pxms / NoteHeight) + "");
                                            file.WriteLine(" V,0," + myTime + "," + myEndTime + ",1," + (float)((float)(Int32.Parse(matches[5].Value) - Int32.Parse(matches[2].Value)) * (float)(pxms / NoteHeight)) + ",1,0");
                                        }
                                        //LN is too Long and need to spilt
                                        else
                                        {
                                            //Head LN
                                            file.WriteLine(" M,0," + (int)(myTime - waitTime) + "," + (int)endOfFirstLN + ",240," + judgementSpace + ",240," + judgementSpace);
                                            file.WriteLine(" V,0," + (int)(myTime - waitTime) + "," + (int)myTime + ",1," + ((2 - beat % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight));
                                            file.WriteLine(" V,0," + (int)(myTime) + "," + (int)endOfFirstLN + ",1," + ((2 - beat % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                            isUp = false;
                                            //Mid LN
                                            while (beatFullLine > 0)
                                            {
                                                if (isUp)
                                                {
                                                    file.WriteLine("Sprite,Overlay,TopCentre,\"SB\\Note1U.png\",320,240");
                                                    file.WriteLine(" F,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN) + ",0,1");
                                                    file.WriteLine(" M,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN + waitTime) + ",240," + (judgementSpace));
                                                    file.WriteLine(" V,0," + (int)(endOfFirstLN - waitTime) + "," + (int)endOfFirstLN + ",1," + ((480 - judgementSpace * 2) / NoteHeight) + "");
                                                    file.WriteLine(" V,0," + (int)(endOfFirstLN) + "," + (int)(endOfFirstLN + waitTime) + ",1," + ((480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                                    endOfFirstLN += waitTime;
                                                    isUp = !isUp;
                                                    beatFullLine--;
                                                }
                                                else
                                                {
                                                    file.WriteLine("Sprite,Overlay,BottomCentre,\"SB\\Note1D.png\",320,240");
                                                    file.WriteLine(" F,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN) + ",0,1");
                                                    file.WriteLine(" M,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN + waitTime) + ",240," + (480 - judgementSpace));
                                                    file.WriteLine(" V,0," + (int)(endOfFirstLN - waitTime) + "," + (int)endOfFirstLN + ",1," + ((480 - judgementSpace * 2) / NoteHeight) + "");
                                                    file.WriteLine(" V,0," + (int)(endOfFirstLN) + "," + (int)(endOfFirstLN + waitTime) + ",1," + ((480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                                    endOfFirstLN += waitTime;
                                                    isUp = !isUp;
                                                    beatFullLine--;
                                                }
                                            }
                                            //End LN
                                            if (isUp)
                                            {
                                                file.WriteLine("Sprite,Overlay,TopCentre,\"SB\\Note1U.png\",320,240");
                                                file.WriteLine(" F,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN) + ",0,1");
                                                file.WriteLine(" M,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(myEndTime) + ",240," + myEndPosition + ",240," + myEndPosition);
                                                file.WriteLine(" V,0," + (int)(endOfFirstLN - waitTime) + "," + (int)endOfFirstLN + ",1," + ((beatEnd % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + "");
                                                file.WriteLine(" V,0," + (int)(endOfFirstLN) + "," + (int)myEndTime + ",1," + ((beatEnd % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                            }
                                            else
                                            {
                                                file.WriteLine("Sprite,Overlay,BottomCentre,\"SB\\Note1D.png\",320,240");
                                                file.WriteLine(" F,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN) + ",0,1");
                                                file.WriteLine(" M,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(myEndTime) + ",240," + (myEndPosition) + ",240," + myEndPosition);
                                                file.WriteLine(" V,0," + (int)(endOfFirstLN - waitTime) + "," + (int)endOfFirstLN + ",1," + ((beatEnd % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + "");
                                                file.WriteLine(" V,0," + (int)(endOfFirstLN) + "," + (int)myEndTime + ",1," + ((beatEnd % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        file.WriteLine("Sprite,Overlay,BottomCentre,\"SB\\Note1D.png\",320,240");
                                        file.WriteLine(" F,0," + (int)(myTime - waitTime) + "," + myTime + ",0,1");

                                        if (beat % 2 + beatLength < 2)
                                        {
                                            //LN that is short enough to be in 1 
                                            file.WriteLine(" M,0," + (int)(myTime - waitTime) + "," + myEndTime + ",240," + myEndPosition + ",240," + myEndPosition);
                                            file.WriteLine(" V,0," + (int)(myTime - waitTime) + "," + myTime + ",1," + (float)((float)(Int32.Parse(matches[5].Value) - Int32.Parse(matches[2].Value)) * pxms / NoteHeight) + "");
                                            file.WriteLine(" V,0," + myTime + "," + myEndTime + ",1," + (float)((float)(Int32.Parse(matches[5].Value) - Int32.Parse(matches[2].Value)) * (float)(pxms / NoteHeight)) + ",1,0");
                                        }
                                        //LN is too Long and need to spilt
                                        else
                                        {
                                            //Head LN
                                            file.WriteLine(" M,0," + (int)(myTime - waitTime) + "," + (int)endOfFirstLN + ",240," + (480 - judgementSpace) + ",240," + (480 - judgementSpace));
                                            file.WriteLine(" V,0," + (int)(myTime - waitTime) + "," + (int)myTime + ",1," + ((2 - beat % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight));
                                            file.WriteLine(" V,0," + (int)(myTime) + "," + (int)endOfFirstLN + ",1," + ((2 - beat % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                            isUp = true;
                                            //Mid LN
                                            while (beatFullLine > 0)
                                            {
                                                if (isUp)
                                                {
                                                    file.WriteLine("Sprite,Overlay,TopCentre,\"SB\\Note1U.png\",320,240");
                                                    file.WriteLine(" F,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN) + ",0,1");
                                                    file.WriteLine(" M,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN + waitTime) + ",240," + (judgementSpace));
                                                    file.WriteLine(" V,0," + (int)(endOfFirstLN - waitTime) + "," + (int)endOfFirstLN + ",1," + ((480 - judgementSpace * 2) / NoteHeight) + "");
                                                    file.WriteLine(" V,0," + (int)(endOfFirstLN) + "," + (int)(endOfFirstLN + waitTime) + ",1," + ((480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                                    endOfFirstLN += waitTime;
                                                    isUp = !isUp;
                                                    beatFullLine--;
                                                }
                                                else
                                                {
                                                    file.WriteLine("Sprite,Overlay,BottomCentre,\"SB\\Note1D.png\",320,240");
                                                    file.WriteLine(" F,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN) + ",0,1");
                                                    file.WriteLine(" M,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN + waitTime) + ",240," + (480 - judgementSpace));
                                                    file.WriteLine(" V,0," + (int)(endOfFirstLN - waitTime) + "," + (int)endOfFirstLN + ",1," + ((480 - judgementSpace * 2) / NoteHeight) + "");
                                                    file.WriteLine(" V,0," + (int)(endOfFirstLN) + "," + (int)(endOfFirstLN + waitTime) + ",1," + ((480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                                    endOfFirstLN += waitTime;
                                                    isUp = !isUp;
                                                    beatFullLine--;
                                                }
                                            }
                                            //End LN
                                            if (isUp)
                                            {
                                                file.WriteLine("Sprite,Overlay,TopCentre,\"SB\\Note1U.png\",320,240");
                                                file.WriteLine(" F,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN) + ",0,1");
                                                file.WriteLine(" M,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(myEndTime) + ",240," + myEndPosition + ",240," + myEndPosition);
                                                file.WriteLine(" V,0," + (int)(endOfFirstLN - waitTime) + "," + (int)endOfFirstLN + ",1," + ((beatEnd % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + "");
                                                file.WriteLine(" V,0," + (int)(endOfFirstLN) + "," + (int)myEndTime + ",1," + ((beatEnd % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                            }
                                            else
                                            {
                                                file.WriteLine("Sprite,Overlay,BottomCentre,\"SB\\Note1D.png\",320,240");
                                                file.WriteLine(" F,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN) + ",0,1");
                                                file.WriteLine(" M,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(myEndTime) + ",240," + (myEndPosition) + ",240," + myEndPosition);
                                                file.WriteLine(" V,0," + (int)(endOfFirstLN - waitTime) + "," + (int)endOfFirstLN + ",1," + ((beatEnd % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + "");
                                                file.WriteLine(" V,0," + (int)(endOfFirstLN) + "," + (int)myEndTime + ",1," + ((beatEnd % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                            }
                                        }
                                    }
                                    break;
                                case "320":
                                    Console.WriteLine("Debug:LN3");
                                    if (isUp)
                                    {
                                        file.WriteLine("Sprite,Overlay,TopCentre,\"SB\\Note1U.png\",320,400");
                                        file.WriteLine(" F,0," + (int)(myTime - waitTime) + "," + myTime + ",0,1");

                                        if (beat % 2 + beatLength < 2)
                                        {
                                            //LN that is short enough to be in 1 
                                            file.WriteLine(" M,0," + (int)(myTime - waitTime) + "," + myEndTime + ",400," + myEndPosition + ",400," + myEndPosition);
                                            file.WriteLine(" V,0," + (int)(myTime - waitTime) + "," + myTime + ",1," + (float)((float)(Int32.Parse(matches[5].Value) - Int32.Parse(matches[2].Value)) * pxms / NoteHeight) + "");
                                            file.WriteLine(" V,0," + myTime + "," + myEndTime + ",1," + (float)((float)(Int32.Parse(matches[5].Value) - Int32.Parse(matches[2].Value)) * (float)(pxms / NoteHeight)) + ",1,0");
                                        }
                                        //LN is too Long and need to spilt
                                        else
                                        {
                                            //Head LN
                                            file.WriteLine(" M,0," + (int)(myTime - waitTime) + "," + (int)endOfFirstLN + ",400," + judgementSpace + ",400," + judgementSpace);
                                            file.WriteLine(" V,0," + (int)(myTime - waitTime) + "," + (int)myTime + ",1," + ((2 - beat % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight));
                                            file.WriteLine(" V,0," + (int)(myTime) + "," + (int)endOfFirstLN + ",1," + ((2 - beat % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                            isUp = false;
                                            //Mid LN
                                            while (beatFullLine > 0)
                                            {
                                                if (isUp)
                                                {
                                                    file.WriteLine("Sprite,Overlay,TopCentre,\"SB\\Note1U.png\",320,400");
                                                    file.WriteLine(" F,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN) + ",0,1");
                                                    file.WriteLine(" M,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN + waitTime) + ",400," + (judgementSpace));
                                                    file.WriteLine(" V,0," + (int)(endOfFirstLN - waitTime) + "," + (int)endOfFirstLN + ",1," + ((480 - judgementSpace * 2) / NoteHeight) + "");
                                                    file.WriteLine(" V,0," + (int)(endOfFirstLN) + "," + (int)(endOfFirstLN + waitTime) + ",1," + ((480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                                    endOfFirstLN += waitTime;
                                                    isUp = !isUp;
                                                    beatFullLine--;
                                                }
                                                else
                                                {
                                                    file.WriteLine("Sprite,Overlay,BottomCentre,\"SB\\Note1D.png\",320,400");
                                                    file.WriteLine(" F,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN) + ",0,1");
                                                    file.WriteLine(" M,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN + waitTime) + ",400," + (480 - judgementSpace));
                                                    file.WriteLine(" V,0," + (int)(endOfFirstLN - waitTime) + "," + (int)endOfFirstLN + ",1," + ((480 - judgementSpace * 2) / NoteHeight) + "");
                                                    file.WriteLine(" V,0," + (int)(endOfFirstLN) + "," + (int)(endOfFirstLN + waitTime) + ",1," + ((480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                                    endOfFirstLN += waitTime;
                                                    isUp = !isUp;
                                                    beatFullLine--;
                                                }
                                            }
                                            //End LN
                                            if (isUp)
                                            {
                                                file.WriteLine("Sprite,Overlay,TopCentre,\"SB\\Note1U.png\",320,400");
                                                file.WriteLine(" F,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN) + ",0,1");
                                                file.WriteLine(" M,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(myEndTime) + ",400," + myEndPosition + ",400," + myEndPosition);
                                                file.WriteLine(" V,0," + (int)(endOfFirstLN - waitTime) + "," + (int)endOfFirstLN + ",1," + ((beatEnd % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + "");
                                                file.WriteLine(" V,0," + (int)(endOfFirstLN) + "," + (int)myEndTime + ",1," + ((beatEnd % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                            }
                                            else
                                            {
                                                file.WriteLine("Sprite,Overlay,BottomCentre,\"SB\\Note1D.png\",320,400");
                                                file.WriteLine(" F,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN) + ",0,1");
                                                file.WriteLine(" M,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(myEndTime) + ",400," + (myEndPosition) + ",400," + myEndPosition);
                                                file.WriteLine(" V,0," + (int)(endOfFirstLN - waitTime) + "," + (int)endOfFirstLN + ",1," + ((beatEnd % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + "");
                                                file.WriteLine(" V,0," + (int)(endOfFirstLN) + "," + (int)myEndTime + ",1," + ((beatEnd % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        file.WriteLine("Sprite,Overlay,BottomCentre,\"SB\\Note1D.png\",320,400");
                                        file.WriteLine(" F,0," + (int)(myTime - waitTime) + "," + myTime + ",0,1");

                                        if (beat % 2 + beatLength < 2)
                                        {
                                            //LN that is short enough to be in 1 
                                            file.WriteLine(" M,0," + (int)(myTime - waitTime) + "," + myEndTime + ",400," + myEndPosition + ",400," + myEndPosition);
                                            file.WriteLine(" V,0," + (int)(myTime - waitTime) + "," + myTime + ",1," + (float)((float)(Int32.Parse(matches[5].Value) - Int32.Parse(matches[2].Value)) * pxms / NoteHeight) + "");
                                            file.WriteLine(" V,0," + myTime + "," + myEndTime + ",1," + (float)((float)(Int32.Parse(matches[5].Value) - Int32.Parse(matches[2].Value)) * (float)(pxms / NoteHeight)) + ",1,0");
                                        }
                                        //LN is too Long and need to spilt
                                        else
                                        {
                                            //Head LN
                                            file.WriteLine(" M,0," + (int)(myTime - waitTime) + "," + (int)endOfFirstLN + ",400," + (480 - judgementSpace) + ",400," + (480 - judgementSpace));
                                            file.WriteLine(" V,0," + (int)(myTime - waitTime) + "," + (int)myTime + ",1," + ((2 - beat % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight));
                                            file.WriteLine(" V,0," + (int)(myTime) + "," + (int)endOfFirstLN + ",1," + ((2 - beat % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                            isUp = true;
                                            //Mid LN
                                            while (beatFullLine > 0)
                                            {
                                                if (isUp)
                                                {
                                                    file.WriteLine("Sprite,Overlay,TopCentre,\"SB\\Note1U.png\",320,400");
                                                    file.WriteLine(" F,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN) + ",0,1");
                                                    file.WriteLine(" M,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN + waitTime) + ",400," + (judgementSpace));
                                                    file.WriteLine(" V,0," + (int)(endOfFirstLN - waitTime) + "," + (int)endOfFirstLN + ",1," + ((480 - judgementSpace * 2) / NoteHeight) + "");
                                                    file.WriteLine(" V,0," + (int)(endOfFirstLN) + "," + (int)(endOfFirstLN + waitTime) + ",1," + ((480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                                    endOfFirstLN += waitTime;
                                                    isUp = !isUp;
                                                    beatFullLine--;
                                                }
                                                else
                                                {
                                                    file.WriteLine("Sprite,Overlay,BottomCentre,\"SB\\Note1D.png\",320,400");
                                                    file.WriteLine(" F,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN) + ",0,1");
                                                    file.WriteLine(" M,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN + waitTime) + ",400," + (480 - judgementSpace));
                                                    file.WriteLine(" V,0," + (int)(endOfFirstLN - waitTime) + "," + (int)endOfFirstLN + ",1," + ((480 - judgementSpace * 2) / NoteHeight) + "");
                                                    file.WriteLine(" V,0," + (int)(endOfFirstLN) + "," + (int)(endOfFirstLN + waitTime) + ",1," + ((480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                                    endOfFirstLN += waitTime;
                                                    isUp = !isUp;
                                                    beatFullLine--;
                                                }
                                            }
                                            //End LN
                                            if (isUp)
                                            {
                                                file.WriteLine("Sprite,Overlay,TopCentre,\"SB\\Note1U.png\",320,400");
                                                file.WriteLine(" F,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN) + ",0,1");
                                                file.WriteLine(" M,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(myEndTime) + ",400," + myEndPosition + ",400," + myEndPosition);
                                                file.WriteLine(" V,0," + (int)(endOfFirstLN - waitTime) + "," + (int)endOfFirstLN + ",1," + ((beatEnd % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + "");
                                                file.WriteLine(" V,0," + (int)(endOfFirstLN) + "," + (int)myEndTime + ",1," + ((beatEnd % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                            }
                                            else
                                            {
                                                file.WriteLine("Sprite,Overlay,BottomCentre,\"SB\\Note1D.png\",320,400");
                                                file.WriteLine(" F,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN) + ",0,1");
                                                file.WriteLine(" M,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(myEndTime) + ",400," + (myEndPosition) + ",400," + myEndPosition);
                                                file.WriteLine(" V,0," + (int)(endOfFirstLN - waitTime) + "," + (int)endOfFirstLN + ",1," + ((beatEnd % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + "");
                                                file.WriteLine(" V,0," + (int)(endOfFirstLN) + "," + (int)myEndTime + ",1," + ((beatEnd % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                            }
                                        }
                                    }
                                    break;
                                case "448":
                                    if (isUp)
                                    {
                                        file.WriteLine("Sprite,Overlay,TopCentre,\"SB\\Note1U.png\",320,240");
                                        file.WriteLine(" F,0," + (int)(myTime - waitTime) + "," + myTime + ",0,1");

                                        if (beat % 2 + beatLength < 2)
                                        {
                                            //LN that is short enough to be in 1 
                                            file.WriteLine(" M,0," + (int)(myTime - waitTime) + "," + myEndTime + ",560," + myEndPosition + ",560," + myEndPosition);
                                            file.WriteLine(" V,0," + (int)(myTime - waitTime) + "," + myTime + ",1," + (float)((float)(Int32.Parse(matches[5].Value) - Int32.Parse(matches[2].Value)) * pxms / NoteHeight) + "");
                                            file.WriteLine(" V,0," + myTime + "," + myEndTime + ",1," + (float)((float)(Int32.Parse(matches[5].Value) - Int32.Parse(matches[2].Value)) * (float)(pxms / NoteHeight)) + ",1,0");
                                        }
                                        //LN is too Long and need to spilt
                                        else
                                        {
                                            //Head LN
                                            file.WriteLine(" M,0," + (int)(myTime - waitTime) + "," + (int)endOfFirstLN + ",560," + judgementSpace + ",560," + judgementSpace);
                                            file.WriteLine(" V,0," + (int)(myTime - waitTime) + "," + (int)myTime + ",1," + ((2 - beat % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight));
                                            file.WriteLine(" V,0," + (int)(myTime) + "," + (int)endOfFirstLN + ",1," + ((2 - beat % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                            isUp = false;
                                            //Mid LN
                                            while (beatFullLine > 0)
                                            {
                                                if (isUp)
                                                {
                                                    file.WriteLine("Sprite,Overlay,TopCentre,\"SB\\Note1U.png\",320,240");
                                                    file.WriteLine(" F,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN) + ",0,1");
                                                    file.WriteLine(" M,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN + waitTime) + ",560," + (judgementSpace));
                                                    file.WriteLine(" V,0," + (int)(endOfFirstLN - waitTime) + "," + (int)endOfFirstLN + ",1," + ((480 - judgementSpace * 2) / NoteHeight) + "");
                                                    file.WriteLine(" V,0," + (int)(endOfFirstLN) + "," + (int)(endOfFirstLN + waitTime) + ",1," + ((480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                                    endOfFirstLN += waitTime;
                                                    isUp = !isUp;
                                                    beatFullLine--;
                                                }
                                                else
                                                {
                                                    file.WriteLine("Sprite,Overlay,BottomCentre,\"SB\\Note1D.png\",320,240");
                                                    file.WriteLine(" F,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN) + ",0,1");
                                                    file.WriteLine(" M,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN + waitTime) + ",560," + (480 - judgementSpace));
                                                    file.WriteLine(" V,0," + (int)(endOfFirstLN - waitTime) + "," + (int)endOfFirstLN + ",1," + ((480 - judgementSpace * 2) / NoteHeight) + "");
                                                    file.WriteLine(" V,0," + (int)(endOfFirstLN) + "," + (int)(endOfFirstLN + waitTime) + ",1," + ((480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                                    endOfFirstLN += waitTime;
                                                    isUp = !isUp;
                                                    beatFullLine--;
                                                }
                                            }
                                            //End LN
                                            if (isUp)
                                            {
                                                file.WriteLine("Sprite,Overlay,TopCentre,\"SB\\Note1U.png\",320,560");
                                                file.WriteLine(" F,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN) + ",0,1");
                                                file.WriteLine(" M,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(myEndTime) + ",560," + myEndPosition + ",560," + myEndPosition);
                                                file.WriteLine(" V,0," + (int)(endOfFirstLN - waitTime) + "," + (int)endOfFirstLN + ",1," + ((beatEnd % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + "");
                                                file.WriteLine(" V,0," + (int)(endOfFirstLN) + "," + (int)myEndTime + ",1," + ((beatEnd % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                            }
                                            else
                                            {
                                                file.WriteLine("Sprite,Overlay,BottomCentre,\"SB\\Note1D.png\",320,560");
                                                file.WriteLine(" F,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN) + ",0,1");
                                                file.WriteLine(" M,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(myEndTime) + ",560," + (myEndPosition) + ",560," + myEndPosition);
                                                file.WriteLine(" V,0," + (int)(endOfFirstLN - waitTime) + "," + (int)endOfFirstLN + ",1," + ((beatEnd % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + "");
                                                file.WriteLine(" V,0," + (int)(endOfFirstLN) + "," + (int)myEndTime + ",1," + ((beatEnd % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        file.WriteLine("Sprite,Overlay,BottomCentre,\"SB\\Note1D.png\",320,560");
                                        file.WriteLine(" F,0," + (int)(myTime - waitTime) + "," + myTime + ",0,1");

                                        if (beat % 2 + beatLength < 2)
                                        {
                                            //LN that is short enough to be in 1 
                                            file.WriteLine(" M,0," + (int)(myTime - waitTime) + "," + myEndTime + ",560," + myEndPosition + ",560," + myEndPosition);
                                            file.WriteLine(" V,0," + (int)(myTime - waitTime) + "," + myTime + ",1," + (float)((float)(Int32.Parse(matches[5].Value) - Int32.Parse(matches[2].Value)) * pxms / NoteHeight) + "");
                                            file.WriteLine(" V,0," + myTime + "," + myEndTime + ",1," + (float)((float)(Int32.Parse(matches[5].Value) - Int32.Parse(matches[2].Value)) * (float)(pxms / NoteHeight)) + ",1,0");
                                        }
                                        //LN is too Long and need to spilt
                                        else
                                        {
                                            //Head LN
                                            file.WriteLine(" M,0," + (int)(myTime - waitTime) + "," + (int)endOfFirstLN + ",560," + (480 - judgementSpace) + ",560," + (480 - judgementSpace));
                                            file.WriteLine(" V,0," + (int)(myTime - waitTime) + "," + (int)myTime + ",1," + ((2 - beat % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight));
                                            file.WriteLine(" V,0," + (int)(myTime) + "," + (int)endOfFirstLN + ",1," + ((2 - beat % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                            isUp = true;
                                            //Mid LN
                                            while (beatFullLine > 0)
                                            {
                                                if (isUp)
                                                {
                                                    file.WriteLine("Sprite,Overlay,TopCentre,\"SB\\Note1U.png\",320,560");
                                                    file.WriteLine(" F,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN) + ",0,1");
                                                    file.WriteLine(" M,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN + waitTime) + ",560," + (judgementSpace));
                                                    file.WriteLine(" V,0," + (int)(endOfFirstLN - waitTime) + "," + (int)endOfFirstLN + ",1," + ((480 - judgementSpace * 2) / NoteHeight) + "");
                                                    file.WriteLine(" V,0," + (int)(endOfFirstLN) + "," + (int)(endOfFirstLN + waitTime) + ",1," + ((480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                                    endOfFirstLN += waitTime;
                                                    isUp = !isUp;
                                                    beatFullLine--;
                                                }
                                                else
                                                {
                                                    file.WriteLine("Sprite,Overlay,BottomCentre,\"SB\\Note1D.png\",320,560");
                                                    file.WriteLine(" F,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN) + ",0,1");
                                                    file.WriteLine(" M,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN + waitTime) + ",560," + (480 - judgementSpace));
                                                    file.WriteLine(" V,0," + (int)(endOfFirstLN - waitTime) + "," + (int)endOfFirstLN + ",1," + ((480 - judgementSpace * 2) / NoteHeight) + "");
                                                    file.WriteLine(" V,0," + (int)(endOfFirstLN) + "," + (int)(endOfFirstLN + waitTime) + ",1," + ((480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                                    endOfFirstLN += waitTime;
                                                    isUp = !isUp;
                                                    beatFullLine--;
                                                }
                                            }
                                            //End LN
                                            if (isUp)
                                            {
                                                file.WriteLine("Sprite,Overlay,TopCentre,\"SB\\Note1U.png\",320,560");
                                                file.WriteLine(" F,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN) + ",0,1");
                                                file.WriteLine(" M,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(myEndTime) + ",560," + myEndPosition + ",560," + myEndPosition);
                                                file.WriteLine(" V,0," + (int)(endOfFirstLN - waitTime) + "," + (int)endOfFirstLN + ",1," + ((beatEnd % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + "");
                                                file.WriteLine(" V,0," + (int)(endOfFirstLN) + "," + (int)myEndTime + ",1," + ((beatEnd % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                            }
                                            else
                                            {
                                                file.WriteLine("Sprite,Overlay,BottomCentre,\"SB\\Note1D.png\",320,560");
                                                file.WriteLine(" F,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(endOfFirstLN) + ",0,1");
                                                file.WriteLine(" M,0," + (int)(endOfFirstLN - waitTime) + "," + (int)(myEndTime) + ",560," + (myEndPosition) + ",560," + myEndPosition);
                                                file.WriteLine(" V,0," + (int)(endOfFirstLN - waitTime) + "," + (int)endOfFirstLN + ",1," + ((beatEnd % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + "");
                                                file.WriteLine(" V,0," + (int)(endOfFirstLN) + "," + (int)myEndTime + ",1," + ((beatEnd % 2) / 2 * (480 - judgementSpace * 2) / NoteHeight) + ",1,0");
                                            }
                                        }
                                    }
                                    break;

                            }
                        }
                    }
                }

                file.WriteLine("Sprite,Overlay,Centre,\"SB\\JudgementBorder.png\",320,240");
                file.WriteLine(" M,0,-5000," + (songLength) + ",320," + judgementSpace);
                file.WriteLine("Sprite,Overlay,Centre,\"SB\\JudgementBorder.png\",320,240");
                file.WriteLine(" M,0,-5000," + (songLength) + ",320," + (480 - judgementSpace));

                file.Write("//Storyboard Sound Samples");
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Copying StoryBoard sprite library...");

            thisPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"SB");
            songPath = Path.GetDirectoryName(path) + "\\SB";
            Copy(thisPath, songPath);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Done!");
            Console.ReadLine();
        }



        public static void Copy(string sourceDirectory, string targetDirectory)
        {
            var diSource = new DirectoryInfo(sourceDirectory);
            var diTarget = new DirectoryInfo(targetDirectory);

            CopyAll(diSource, diTarget);
        }

        public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }
    }
}
