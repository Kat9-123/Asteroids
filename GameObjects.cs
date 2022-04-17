// Asteroids | By: Kat9_123
using System;


// I decided to store all gameobjects in the same file, because their behaviours are quite simple.
namespace Asteroids
{
    class Asteroid : GameObject
    {

        private Vector direction;
        private float rotSpeed;


        // Size (2 = large, 1 = medium, 0 = small)
        public int type;


        public Asteroid(int _type = 2)
        {
            Random rng = new Random();
            type = _type;
            int p = 0;

            // Choose a random polygon depending on the type of asteroid.

            switch(type)
            {
                // Big
                case 2:
                    
                    p = rng.Next(0,2);
                    if (p == 0)
                    {
                        polygon = new Polygon(new Vector[] {
                            new Vector(8,3),
                            new Vector(11,5),
                            new Vector(15,10),
                            new Vector(17,13),
                            new Vector(15,16),
                            new Vector(11,18),
                            new Vector(7,18),
                            new Vector(0,12),
                        });
                    }
                    if (p == 1)
                    {
                        polygon = new Polygon(new Vector[] {
                            new Vector(3,4),
                            new Vector(10,2),
                            new Vector(16,5),
                            new Vector(15,11),
                            new Vector(16,16),
                            new Vector(6,18),
                            new Vector(0,14),
                            new Vector(4,9),
                        });
                    }

                    character = '2';
                    break;

                // Medium
                case 1:
                    p = rng.Next(0,2);

                    if (p == 0)
                    {
                        polygon = new Polygon(new Vector[] {
                            new Vector(1,2),
                            new Vector(5,1),
                            new Vector(9,4),
                            new Vector(4,9),
                            new Vector(0,5)
                        });
                    }
                    if (p == 1)
                    {
                        polygon = new Polygon(new Vector[] {
                            new Vector(2,2),
                            new Vector(6,0),
                            new Vector(9,3),
                            new Vector(6,6),
                            new Vector(7,11),
                            new Vector(1,10),
                            new Vector(0,5),

                        });
                    }

                    character = '1';
                    break;

                // Small
                case 0:
                    p = rng.Next(0,2);
                    if (p == 0)
                    {
                        polygon = new Polygon(new Vector[] {
                            new Vector(3,1),
                            new Vector(5,4),
                            new Vector(4,5),
                            new Vector(2,6),
                            new Vector(0,3),

                        });
                    }
                    if (p == 1)
                    {
                        polygon = new Polygon(new Vector[] {
                            new Vector(2,1),
                            new Vector(4,0),
                            new Vector(7,3),
                            new Vector(4,5),
                            new Vector(0,4),

                        });              
                    }

                    character = '0';
                    break;


            }


            // Randomly generate movement and rotation
            direction.x = 15 - ((float) (rng.NextDouble()*30));
            direction.y = 15 - ((float) (rng.NextDouble()*30));
            rotation = (float)(rng.NextDouble()*360);
            rotSpeed = (float)(rng.NextDouble()*70);

            largestSize = polygon.GetLargestSize();
            
        }

        // Apply rotation and movement
        public override void Update(float deltaTime)
        {
            position.x += direction.x * deltaTime;
            position.y += direction.y * deltaTime;
            rotation += rotSpeed*deltaTime;
        }


    }
    class Bullet : GameObject
    {
        private Vector direction = new Vector(100,0);

        public Bullet(GameObject player)
        {
            polygon = new Polygon(new Vector[] {
                new Vector(0,0),
                new Vector(2,0),
                new Vector(2,2),
                new Vector(0,2)
                
            });
            character = 'X';
            position = player.position;

            // The bullet spawns inside the player. This isnt too bad since its pretty fast.
            // So didnt offset it correctly

            direction = direction.Rotate(player.rotation-90);
            largestSize = polygon.GetLargestSize();

        }


        public override void Update(float deltaTime)
        {
            position.x += direction.x * deltaTime;
            position.y += direction.y * deltaTime;
        }
    }

    class Player : GameObject 
    {
        private static Vector playerMotion = new Vector();
        public Player()
        {

            polygon = new Polygon(new Vector[] {
                new Vector(3,0),
                new Vector(4,0),
                new Vector(6,8),
                new Vector(0,8)
            });
            character = '#';
            Reset();
            largestSize = polygon.GetLargestSize();
            

        }

        // Reset the player 
        public void Reset()
        {
            position = new Vector(Utils.SCREEN_SIZE_X/2,Utils.SCREEN_SIZE_Y/2);
            rotation = 0f;
            playerMotion = new Vector(0,0);
            
        }

        

        // The player's update function does not mach the signature of the base Update function
        // So... yeah
        public void Update(bool wPressed, bool aPressed, bool dPressed,float deltaTime)
        {
            Vector playerMovementVector = new Vector();



            // Get input
            if(wPressed) {playerMovementVector.y = -1;}

            if(aPressed) rotation -= 210*deltaTime;

            if(dPressed) rotation += 210*deltaTime;


            // Rotate the input vector depending on the rotation of the player
            playerMovementVector = playerMovementVector.Rotate(rotation-90);
   
            

            // Check if the player is not standing still. This is to prevent division by zero
            if (!(playerMovementVector.x == 0 && playerMovementVector.y == 0))
            {
                float distance = (float) Math.Sqrt(playerMovementVector.x * playerMovementVector.x + playerMovementVector.y * playerMovementVector.y);
                Vector motion = new Vector(playerMovementVector.x / distance, playerMovementVector.y / distance);
                motion.x *= 1;
                motion.y *= 1;

                playerMotion.x += motion.x;
                playerMotion.y += motion.y;
            }

            // Slow the player down
            playerMotion.x -= playerMotion.x/80;
            playerMotion.y -= playerMotion.y/80;


            // Apply motion
            position.x += playerMotion.x*deltaTime;
            position.y += playerMotion.y*deltaTime;
        }



    }


}