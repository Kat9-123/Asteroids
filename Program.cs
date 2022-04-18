// Asteroids | By: Kat9_123

using System;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;


namespace Asteroids
{
    static class Program
    {
        static void Main(string[] args)
        {
            // Prepare the console

            Console.Title = "Asteroids | By: Kat9_123";

            // I thought that Courier New looked alright
            ConsoleHelper.SetCurrentFont(Utils.FONT, 7);
            
            // These were the values that worked.
            Console.SetWindowSize(Utils.SCREEN_SIZE_X + 1,Utils.SCREEN_SIZE_Y + 2);
            Console.SetBufferSize(Utils.SCREEN_SIZE_X + 1,Utils.SCREEN_SIZE_Y + 2);
            Console.CursorVisible = false;

            // Load the highscore, if the file is present.
            if (File.Exists(Directory.GetCurrentDirectory() + "\\score"))
            {
                GameManager.highScore = Utils.LoadData();
            }


            Renderer.Initialise();
          
            GameManager.Start();


        }

    }

    static class GameManager
    {

        // I know that GetAsyncKeyState is a very old way of doing input 
        // and that it will probably trigger keylogger detection but I don't think that it really matters. 
        // It also detects input even though the window is not in focus, but again, that doesnt really matter.
        // It just works and its very simple.
        [DllImport("user32.dll")]
        private static extern int GetAsyncKeyState(int vKeys);

        public static List<GameObject> gameObjects = new List<GameObject>();

        public static int score = 0;
        public static int highScore = 0;

        public static Player player;

        public static List<Asteroid> asteroids = new List<Asteroid>();
        public static List<Bullet> bullets = new List<Bullet>();


        // GameObjects queued for destruction before the next update.
        public static List<GameObject> destructionQueue = new List<GameObject>();

        
        // Possible framerate limiting functionality?
        // private const float FRAME_DURATION = 1/60f; 



        // Pause at beginning of the game.
        private static bool started = false;


        // Variables for delay between bullets
        private static float bulletTimer;
        private const float BULLET_DELAY = 0.4f;
    
        private static bool paused = false;
        private static bool justPaused = false;


        // Destroy queued objects 
        // Remove them from their list(s), thus fully dereferencing them. 
        // The C# garbage collecter takes care of the rest
        public static void DestroyQueuedObjects()
        {
            for (int obj = 0; obj < destructionQueue.Count; obj++)
            {
                gameObjects.Remove(destructionQueue[obj]);
                

                if (destructionQueue[obj] is Bullet)
                {
                    bullets.Remove((Bullet) destructionQueue[obj]);

                }
                else if (destructionQueue[obj] is Asteroid)
                {
                    asteroids.Remove((Asteroid) destructionQueue[obj]);
                }
                else if (destructionQueue[obj] is Player)
                {
                    //
                }

            }
            destructionQueue.Clear();
        }
        public static void Start()
        {

            // Set all variables to their defaults. This is required for a restart functionality
            gameObjects = new List<GameObject>();
            bullets = new List<Bullet>();
            asteroids = new List<Asteroid>();
            player = null;
            started = false;
            bulletTimer = 0f;
            score = 0;
            paused = false;

            // Used for calculating deltaTime.
            double previousTime = 0f;

            Utils.Instance(new Player());  
    
            // I read that the stopwatch class should work well enough to calculate deltaTime.
            Stopwatch watch = new Stopwatch();
            watch.Start();

            // Main gameloop
            while(true)
            {
                // Exit when escape is pressed
                if (GetAsyncKeyState(Utils.VK_ESC) > 1)
                {
                    Utils.SaveData(highScore);
                    Environment.Exit(1);
                }
                // Check if the pause key (P) was JUST pressed
                if((GetAsyncKeyState(Utils.VK_P) > 1))
                {
                    if (!justPaused)
                    {
                        paused = !paused;
                        justPaused = true;
                        previousTime = 0f;
                    }
                }
                else {justPaused = false;}

                if (paused)
                {
                    // Write PAUSED to the centre of the screen (ish)
                    Console.SetCursorPosition(Utils.SCREEN_SIZE_X/2 - 2, Utils.SCREEN_SIZE_Y /2);
                    Console.WriteLine("PAUSED");

                    Thread.Sleep(50);
                    continue;
                }

                // Calculate deltaTime
                long now = watch.ElapsedMilliseconds;
                if (previousTime == 0) previousTime = now; 
                double deltaTime = (now - previousTime)/1000.0;
                previousTime = now;

                DestroyQueuedObjects();

                
                Update((float)deltaTime);

                if(Utils.SHOW_FPS) Console.Title = "Asteroids | By: Kat9_123 | FPS: " + ((int) (1/deltaTime)).ToString();

                Renderer.Draw(Renderer.Render(gameObjects));
                
                Physics.TestBulletCollision(bullets,asteroids);

                // Pause before starting
                if (!started)
                {
                    Console.ReadKey(true);
                    started = true;
                }
            }
            

        }



        public static void Update(float deltaTime)
        {

            

            // Update all objects. Ideally this should all be in one loop, but because the player is special
            // (it needs more arguments) this cant be done. Is this a design flaw? probably. Does it matter
            // for a small game like this? not really.
            
            // Depending on the current control buttons pressed, ask the player to move
            player.Update(
                (GetAsyncKeyState(Utils.VK_W) > 1) || (GetAsyncKeyState(Utils.VK_UP) > 1), // W || UP
                (GetAsyncKeyState(Utils.VK_A) > 1) || (GetAsyncKeyState(Utils.VK_LEFT) > 1), // A || LEFT
                (GetAsyncKeyState(Utils.VK_D) > 1) || (GetAsyncKeyState(Utils.VK_RIGHT) > 1), // D || RIGHT
                deltaTime
            );

            // Asteroid Update
            for (int ast = 0; ast < asteroids.Count; ast++)
            {
                asteroids[ast].Update(deltaTime);
            }

            // Bullet Update
            for (int bullet = 0; bullet < bullets.Count; bullet++)
            {
                bullets[bullet].Update(deltaTime);
            }
            

            // If there arent enough asteroids, spawn more
            if (asteroids.Count < 6)
            {
                SpawnAsteroid();
            }


            // Shooting logic
            if(bulletTimer > 0) bulletTimer -= deltaTime;

            if ((GetAsyncKeyState(Utils.VK_SPACE) > 1) && bulletTimer <= 0)
            {
                Utils.Instance(new Bullet(player));
                bulletTimer = BULLET_DELAY;
            }


            // High score logic
            if (score > highScore)
            {
                highScore = score;
            }


            // Questionable screenwrap. It works well for the player, which is the most important.
            // If it's a bullet, just destroy it.
            for (int gameObject = 0; gameObject < gameObjects.Count; gameObject++)
            {
                GameObject obj = gameObjects[gameObject];
                Vector size = obj.largestSize;

                if (obj.position.x < -size.x) 
                {
                    if(obj is Bullet) {obj.Destroy(); continue;}
                    obj.position.x = Utils.SCREEN_SIZE_X+1;
                }

                if (obj.position.x > Utils.SCREEN_SIZE_X+1) 
                {
                    if(obj is Bullet) {obj.Destroy(); continue;}
                    obj.position.x = -size.x;
                }

                if (obj.position.y < -size.y)
                {
                    if(obj is Bullet) {obj.Destroy(); continue;}
                    obj.position.y = Utils.SCREEN_SIZE_Y+2;
                } 
                if (obj.position.y > Utils.SCREEN_SIZE_Y+2) 
                {
                    if(obj is Bullet) {obj.Destroy(); continue;}
                    obj.position.y = -size.y;
                }
            }


        }

        // Spawn an asteroid at the edge of the map
        public static void SpawnAsteroid()
        {
            Random rng = new Random();
            int side = rng.Next(0,4);

            Vector pos = new Vector();

            switch(side)
            {
                // Left
                case 0:
                    pos.x = -30;
                    pos.y = (float) (rng.NextDouble()*Utils.SCREEN_SIZE_Y);
                    break;
                
                // Top
                case 1:
                    pos.y = -18;
                    pos.x = (float) (rng.NextDouble()*Utils.SCREEN_SIZE_X);
                    break;

                // Right
                case 2:
                    pos.x = Utils.SCREEN_SIZE_X + 30;
                    pos.y = (float) (rng.NextDouble()*Utils.SCREEN_SIZE_Y);
                    break;

                // Bottom
                case 3:
                    pos.y = Utils.SCREEN_SIZE_Y + 18;
                    pos.x = (float) (rng.NextDouble()*Utils.SCREEN_SIZE_X);
                    break;
            }

            Asteroid ast = Utils.Instance(new Asteroid());
            ast.position = pos;

        }

        public static void GameOver()
        {
            // Possible to increase font size when the game endS to make the Gameover text
            // actualy readable.
            // ConsoleHelper.SetCurrentFont(Utils.FONT, 17);

  
            
            Utils.SaveData(highScore);

            // Very hacky. I want to keep the UI without the GameObjects.
            char[,] image = Renderer.EmptyImage();
            image = Renderer.AddUI(image);
            Renderer.Draw(image);


            // Centred text
            Console.SetCursorPosition(Utils.SCREEN_SIZE_X / 2 - 7, Utils.SCREEN_SIZE_Y / 2 - 2);
            Console.WriteLine("GAME OVER!");
            Console.SetCursorPosition(Utils.SCREEN_SIZE_X / 2 - 12, Utils.SCREEN_SIZE_Y / 2 - 1);
            Console.WriteLine("Press R to try again");


            
            while (true)
            {
                // Restart if the R key is pressed.
                if (GetAsyncKeyState(Utils.VK_R) > 1) Start();
                
                // Or exit
                if (GetAsyncKeyState(Utils.VK_ESC) > 1)
                {
                    Utils.SaveData(highScore);
                    Environment.Exit(1);
                }

                Thread.Sleep(50);

            }

        }


    }


}
