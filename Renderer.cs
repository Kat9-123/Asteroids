// Asteroids | By: Kat9_123
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Asteroids
{
    static class Renderer
    {
        // This magic is from stackoverflow.com/questions/2754518/how-can-i-write-fast-colored-output-to-console
        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern SafeFileHandle CreateFile(
            string fileName,
            [MarshalAs(UnmanagedType.U4)] uint fileAccess,
            [MarshalAs(UnmanagedType.U4)] uint fileShare,
            IntPtr securityAttributes,
            [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
            [MarshalAs(UnmanagedType.U4)] int flags,
            IntPtr template);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteConsoleOutputW(
        SafeFileHandle hConsoleOutput, 
        CharInfo[] lpBuffer, 
        Coord dwBufferSize, 
        Coord dwBufferCoord, 
        ref SmallRect lpWriteRegion);

        [StructLayout(LayoutKind.Sequential)]
        public struct Coord
        {
            public short X;
            public short Y;

            public Coord(short _X, short _Y)
            {
                X = _X;
                Y = _Y;
            }
        };

        [StructLayout(LayoutKind.Explicit)]
        public struct CharUnion
        {
            [FieldOffset(0)] public ushort UnicodeChar;
            [FieldOffset(0)] public byte AsciiChar;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct CharInfo
        {
            [FieldOffset(0)] public CharUnion Char;
            [FieldOffset(2)] public short Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SmallRect
        {
            public short Left;
            public short Top;
            public short Right;
            public short Bottom;
        }

        private static SafeFileHandle safeFileHandle;

        // Dark magic
        [STAThread]
        public static void Initialise()
        {
            safeFileHandle = CreateFile("CONOUT$", 0x40000000, 2, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);
        }



        // Generate a full buffer of whitespaces
        public static char[,] EmptyImage()
        {
            char[,] result = new char[Utils.SCREEN_SIZE_Y,Utils.SCREEN_SIZE_X];
            for (int y = 0; y < Utils.SCREEN_SIZE_Y; y++)
            {
                for (int x = 0; x < Utils.SCREEN_SIZE_X; x++)
                {
                    result[y,x] = ' ';
                }
            }
            return result;
        }

        // Render the gameobjects to a buffer
        public static char[,] Render(List<GameObject> gameObjects)
        {

            // Clean buffer
            char[,] result = EmptyImage();

    
            // Make an array of all of the current polygons. 
            // Also apply all position and rotations
            Polygon[] polygons = new Polygon[gameObjects.Count];
            for (int i = 0; i < gameObjects.Count; i++)
            {
                polygons[i] = gameObjects[i].polygon.OffsetPolygon(gameObjects[i].position, gameObjects[i].rotation);
            }


            // Rasterisation process
            for (int y = 0; y < Utils.SCREEN_SIZE_Y; y++)
            {
                for (int x = 0; x < Utils.SCREEN_SIZE_X; x++)
                {
                    for (int i = 0; i < polygons.Length; i++)
                    {
                        // Optimisation. If the object is too far away dont even bother
                        // Checking if the pixel is inside of it.
                        if (Math.Abs(gameObjects[i].position.x - x) > gameObjects[i].largestSize.x) continue;
                        if (Math.Abs(gameObjects[i].position.y - y) > gameObjects[i].largestSize.y) continue;

                        // Check if current pixel lies within the current polygon
                        // Add 0.5f to check the centre of the pixel
                        if (Physics.IsPointInsidePolygon(polygons[i],new Vector(x+0.5f,y+0.5f)))
                        {
                            if (!gameObjects[i].visible) continue;


                            // Very questionable physics
                            Physics.TestPlayerCollision(result[y,x],gameObjects[i].character);

                            
                            // Place the character
                            result[y,x] = gameObjects[i].character;
                        }
                    }
                }
            }

            // Apply the UI
            result = AddUI(result);

 
            return result;
        }
        
        // Adapted form: stackoverflow.com/questions/2754518/how-can-i-write-fast-colored-output-to-console
        // Though printing is normally fast enough, its very slow when using different colours
        [STAThread]
        public static void Draw(char[,] image)
        {
            if (!safeFileHandle.IsInvalid)
            {
                CharInfo[] buf = new CharInfo[Utils.SCREEN_SIZE_X * Utils.SCREEN_SIZE_Y];
                SmallRect rect = new SmallRect() { Left = 0, Top = 0, Right = Utils.SCREEN_SIZE_X, Bottom = Utils.SCREEN_SIZE_Y };

  
                for (int y = 0; y < Utils.SCREEN_SIZE_Y; y++)
                {
                    for (int x = 0; x < Utils.SCREEN_SIZE_X; x++)
                    {
                        // Move character to new buffer
                        buf[y*Utils.SCREEN_SIZE_X + x].Char.AsciiChar = Convert.ToByte(image[y,x]);

                        // Colours
                        short col = 0;
                        switch(image[y,x])
                        {

                            case Utils.PLAYER_CHARACTER:
                                col = 2; // Green
                                break;
                            case Utils.BULLET_CHARACTER:
                                col = 4; // Red
                                break;
                            
                            case Utils.BIG_ASTEROID_CHARACTER:
                            case Utils.MEDIUM_ASTEROID_CHARACTER:
                            case Utils.SMALL_ASTEROID_CHARACTER:
                                col = 15; // White
                                break;

                            case Utils.HIGHSCORE_CHARACTER:
                                col = 8; // Grey
                                break;
                            
                            case Utils.SCORE_CHARACTER:
                                col = 7; // Orange
                                break;
                        }  
                        buf[y*Utils.SCREEN_SIZE_X + x].Attributes = col;


                    }
                }

                


                // Write to screen
                bool b = WriteConsoleOutputW(safeFileHandle, buf,
                    new Coord() { X = Utils.SCREEN_SIZE_X, Y = Utils.SCREEN_SIZE_Y },
                    new Coord() { X = 0, Y = 0 },
                    ref rect
                );
            }




            Console.SetCursorPosition(0,0);

        }
        
        // Hacked in UI module
        public static char[,] AddUI(char[,] image)
        {
            // Score
            string score = GameManager.score.ToString();
            for (int i = 0; i < score.Length; i++)
            {
                image = AddNumber(image,(int) Char.GetNumericValue(score[i]),i,0);
            }


            // if (highScore == score) return image;
            
            // High score
            string highScore = GameManager.highScore.ToString();
            for (int i = 0; i < highScore.Length; i++)
            {
                image = AddNumber(image,(int) Char.GetNumericValue(highScore[i]),i,1);
            }
            

            return image;
        }

        private static char[,] AddNumber(char[,] image, int n, int index,int type)
        {
            // Go over every pixel in a number
            for (int y = 0; y < 5; y++)
            {
                for (int x = 0; x < 7; x++)
                {
                    // Each number is 35 characters long.
                    if (Utils.NUMBERS[y*7 + x + 35*n] != ' ')
                    {
                        // Score
                        if (type == 0)
                        {
                            // Place the correct pixel from the correct number at the correct location
                            // and add some offsets
                            image[y+1 + 6*type,x+1+index*7] = 'S';
                        }
                        // High score
                        else if (type == 1)
                        {
                            // Place the correct pixel from the correct number at the correct location
                            // and add some offsets     
                            image[y+1 + 6*type,x+1+index*7] = 'H';
                        }
                        
                    }
                    
                }
            }
            return image;
        }

    }





    

}