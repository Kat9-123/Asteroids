// Asteroids | By: Kat9_123
using System;
using System.IO;



namespace Asteroids
{

    // This class contains the "settings", though that doesn't mean much in an open source game like this.
    // You can definitely play around with these values, if you want.
    static class Settings
    {
        // A bunch of constants. You can change these if you want
        public const bool SHOW_FPS = true;

        // 0 = Normal mode, 1 = Thin outlines, 2 = Thick outlines
        // It does reduce performance, so be wary
        public const short OUTLINE_MODE = 0;
        

        public const string FONT = "Courier New";

        public const bool DECELERATE = true;


        public const char PLAYER_CHARACTER = '#';

        public const char BULLET_CHARACTER = 'X';

        public const char BIG_ASTEROID_CHARACTER = '2';
        public const char MEDIUM_ASTEROID_CHARACTER = '1';
        public const char SMALL_ASTEROID_CHARACTER = '0';

        public const char HIGHSCORE_CHARACTER = 'H';
        public const char SCORE_CHARACTER = 'S';


        public const int SCREEN_SIZE_X = 150;
        public const int SCREEN_SIZE_Y = 80;

    }

    // All utility (and some other things) functions and variables.
    static class Utils
    {
    


        // Keycodes for GetAsyncKeyState
        public const short VK_W = 0x57;
        public const short VK_A = 0x41;
        public const short VK_D = 0x44;

        public const short VK_UP = 0x26;

        public const short VK_LEFT = 0x25;
        public const short VK_RIGHT = 0x27;

        public const short VK_P = 0x50;
        public const short VK_ESC = 0x1B;
        public const short VK_R = 0x52;

        public const short VK_SPACE = 0x20;



        // "Font" for the score displays. Based on: gist.github.com/yuanqing/ffa2244bd134f911d365
        public const string NUMBERS = " 000000 00  00 00  00 00  00 000000" + // 0
                                      " 0000     00     00     00   000000" + // 1
                                      " 000000     00 000000 00     000000" + // 2
                                      " 000000     00 000000     00 000000" + // 3
                                      " 00  00 00  00 000000     00     00" + // 4
                                      " 000000 00     000000     00 000000" + // 5
                                      " 000000 00     000000 00  00 000000" + // 6
                                      " 000000     00     00     00     00" + // 7
                                      " 000000 00  00 000000 00  00 000000" + // 8
                                      " 000000 00  00 000000     00 000000";  // 9









        // Save the current high score in an xor encrypted file. It also has a simple checksum
        // Kind of overengineerd but I wanted to make a simple system like this.
        public static void SaveData(int data)
        {
            Random rng = new Random();

            // Subtract one just to be safe
            int key = rng.Next(0,Int32.MaxValue - 1);

            // "Encryption"
            data ^= key;

            // Checksum
            int checkInt = Math.Abs(key + data);


            // Write to file. Seperate variables with 'H' (dont ask why)
            File.WriteAllText(Directory.GetCurrentDirectory() + "\\score", (checkInt.ToString("X") + "H" + key.ToString("X") + "H" + data.ToString("X")));


            
        }

        // Load high score
        public static int LoadData()
        {
            string data = File.ReadAllText(Directory.GetCurrentDirectory() + "\\score");

            // The data is stored in a 'H'-seperated format
            string[] dataList = data.Split('H');

            int checkInt = int.Parse(dataList[0], System.Globalization.NumberStyles.HexNumber);
            int key = int.Parse(dataList[1], System.Globalization.NumberStyles.HexNumber);
            int n = int.Parse(dataList[2], System.Globalization.NumberStyles.HexNumber);

            // If the checksum is invalid, shame the cheater.
            if (checkInt != Math.Abs(key + n))
            {
                ConsoleHelper.SetCurrentFont(Settings.FONT,200);
                Console.SetWindowSize(7,1);
                Console.SetBufferSize(8,1);
                Console.Title = "Really?";
                Console.Write("Really?");
                Console.ReadKey();
                Environment.Exit(0);
            }

            // "Decryption"
            return n ^ key;
        }


        // There is probably a way to do this so that its scalable, but for this game it doesnt matter
        public static Bullet Instance(Bullet obj)
        {
            GameManager.gameObjects.Add(obj);
            GameManager.bullets.Add(obj);
            return obj;
        }

        public static Player Instance(Player obj)
        {
            GameManager.gameObjects.Add(obj);
            GameManager.player = obj;
            return obj;
        }

        public static Asteroid Instance(Asteroid obj)
        {
            GameManager.gameObjects.Add(obj);
            GameManager.asteroids.Add(obj);
            return obj;
        }


    }

    
    // Polar coordinates (radius and angle)
    struct PolarVector
    {
        public double radius;
        public double theta;

        public PolarVector(double _radius,double _theta)
        {
            radius = _radius;
            theta = _theta;
        }

        public Vector PolarToCartesian()
        {
            Vector vec = new Vector();
            
            vec.x = (float) (radius * Math.Cos(theta));
            vec.y = (float) (radius * Math.Sin(theta));

            return vec;

        }


    }
    // A polygon is a list of vectors, where each vector "connects" to the next one.
    // The last vector "connects" to the first.
    struct Polygon
    {
        public Vector[] vectors;

        public Polygon(Vector[] _vectors)
        {
            vectors = _vectors;
        }

        // Find the centre of the polygon by taking the largest x and y (and dividing by two)
        public Vector FindCentreOfPolygon()
        {
            Vector centre = GetPolygonSize();

            centre.x /= 2f;
            centre.y /= 2f;

            return centre;
        }

        // Get the largest possible vector that a polygon can have. Accounting for rotation
        public Vector GetLargestSize()
        {
            float l = 0;
            for (int vec = 0; vec < vectors.Length; vec++)
            {
                if (vectors[vec].x > l) l = vectors[vec].x;
                if (vectors[vec].y > l) l = vectors[vec].y;
            }

            return new Vector(l,l);          
        }

        // Find the size of the polygon by finding the largest x and y values.
        public Vector GetPolygonSize()
        {
            Vector size = new Vector(0,0);
            for (int vec = 0; vec < vectors.Length; vec++)
            {
                if (vectors[vec].x > size.x) size.x = vectors[vec].x;
                if (vectors[vec].y > size.y) size.y = vectors[vec].y;
            }

            return size;

        }

        // Move and rotate a polygon by the given amounts
        public Polygon OffsetPolygon(Vector position, float rotation)
        {

            // Create new polygon
            Polygon poly = new Polygon();
            poly.vectors = new Vector[vectors.Length];


            // Each vector in the polygon
            for (int vec = 0; vec < vectors.Length; vec++)
            {
                // First apply rotation using polar coordinates. (Idk if this is the correct way to do it)
                // Also add an offset to rotate around the centre instead of the top left
                Vector offSetVec = FindCentreOfPolygon();
                Vector offsettedVec = new Vector();

                // Offset
                offsettedVec.x = vectors[vec].x - offSetVec.x;
                offsettedVec.y = vectors[vec].y - offSetVec.y;

                // Rotation
                PolarVector polVec = new PolarVector();

                polVec = offsettedVec.CartesianToPolar();

                polVec.theta += (rotation*Math.PI) / 180;

                poly.vectors[vec] = polVec.PolarToCartesian();
                
                // Remove offset again
                poly.vectors[vec].x += offSetVec.x;
                poly.vectors[vec].y += offSetVec.y;


                // Add position
                poly.vectors[vec].x += position.x - FindCentreOfPolygon().x / 2.0f;
                poly.vectors[vec].y += position.y - FindCentreOfPolygon().y / 2.0f;





            }
            return poly;
        }
    }

    struct Vector
    {
        public float x;
        public float y;

        public Vector(float _x,float _y)
        {
            x = _x;
            y = _y;
        }

        // Rotate a vector around (0,0). I dont know if this is the correct way to do this. 
        public Vector Rotate(float angle)
        {
            PolarVector polVec = CartesianToPolar();

            polVec.theta = (angle*Math.PI) / 180;

            return polVec.PolarToCartesian();
        }

        public PolarVector CartesianToPolar()
        {
            PolarVector polVec = new PolarVector();

            polVec.radius = Math.Sqrt((x*x) + (y*y));
            polVec.theta = Math.Atan2(y, x);

            return polVec;
        }
    }

    class GameObject
    {

        public Vector largestSize;
        public Polygon polygon;
        public Vector position;

        public float rotation;

        public char character;

        public bool visible = true;


        public void Destroy()
        {
            GameManager.destructionQueue.Add(this);
        }


        public virtual void Update(float deltaTime) {}

    
    }
}
