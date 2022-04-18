// Asteroids | By: Kat9_123
using System;
using System.Collections.Generic;

namespace Asteroids
{

    // A questionable solution for 2d physics.
    static class Physics
    {

        // Detect collision by checking if an asteroid wants to render on top of the player
        // This is highly questionable, but it works and is fast. 
        // Why do the physics again if the rasteriser already does it?
        // (Yes it does mean that offscreen objects dont collide, but I think that thats a good thing
        // if feels more fair)
        public static void TestPlayerCollision(char a, char b)
        {
            if (a == Utils.PLAYER_CHARACTER && 
                (b == Utils.BIG_ASTEROID_CHARACTER || b == Utils.MEDIUM_ASTEROID_CHARACTER || b == Utils.SMALL_ASTEROID_CHARACTER)
                )
            {
                GameManager.GameOver();
            }
        
        }


        // Sadly we can't use the other detection method because we need to know
        // which bullet and which asteroid collide. This uses the same algorithm the rasteriser uses.
        // It assumes that bullets are points.
        public static void TestBulletCollision(List<Bullet> bullets, List<Asteroid> asteroids)
        {
            for (int bullet = 0; bullet < bullets.Count; bullet++)
            {
                for (int asteroid = 0; asteroid < asteroids.Count; asteroid++)
                {
                    // Since the bullet gets destroyed AFTER the frame has rendered
                    // make it invisible and check if it is visible (which goes in to effect immediately)
                    // So bullets can only collide once (its a bit hacky but it works)
                    if (!bullets[bullet].visible) continue;

                    // Apply offsets
                    Polygon poly = asteroids[asteroid].polygon.OffsetPolygon(asteroids[asteroid].position,asteroids[asteroid].rotation);
                    
                    // Check for collision
                    if (IsPointInsidePolygon(poly,bullets[bullet].position))
                    {
                        // Make bullet invisible so it cant collide more than once
                        bullets[bullet].visible = false;

                        bullets[bullet].Destroy();
                        asteroids[asteroid].Destroy();

                        switch(asteroids[asteroid].type)
                        {
                            case 2:
                                GameManager.score += 20;
                                break;
                            case 1:
                                GameManager.score += 50;
                                break;
                            case 0:
                                GameManager.score += 100;
                                break;
                        }

                        // If the asteroid is of the smallest type, dont spawn any new asteroids
                        if (asteroids[asteroid].type == 0) continue;


                        Random rng = new Random();

                        // Spawn new asteroids
                        for (int i = 0; i < rng.Next(2,4); i++)
                        {
                            Asteroid ast = Utils.Instance(new Asteroid(asteroids[asteroid].type-1));
                            ast.position = asteroids[asteroid].position;
                            ast.position.x += 4 - (float) (rng.NextDouble()*8);
                            ast.position.y += 4 - (float) (rng.NextDouble()*8);
                        }
                        

                    }
                }
            }
        }





        // stackoverflow.com/questions/3838329/how-can-i-check-if-two-segments-intersect
        private static bool Ccw(Vector a, Vector b, Vector c)
        {
            return (c.y-a.y) * (b.x-a.x) > (b.y-a.y) * (c.x-a.x);
        }


        // I chose to ignore collinearity and stuff because those things should be very rare.
        private static bool Intersect(Vector point1, Vector point2, Vector line1, Vector line2)
        {
            return (Ccw(point1,line1,line2) != Ccw(point2,line1,line2)) && (Ccw(point1,point2,line1) != Ccw(point1,point2,line2));
        }


        public static bool IsPointInsidePolygon(Polygon poly, Vector p)
        {   

            Vector[] polygon = poly.vectors;

            // Create a Vector for line segment from p to some large number
            Vector extreme = new Vector(10000, p.y);
            
            int count = 0;
            // Every line
            for (int startIndex = 0; startIndex < polygon.Length; startIndex++)
            {
                
                Vector start = polygon[startIndex];
                Vector end = new Vector();
                
                // Connect last and first vectors
                if (startIndex == polygon.Length - 1)
                {
                    end = polygon[0];
                }
                // Else connect to the next vector
                else { end = polygon[startIndex+1]; }
                
                // Check if there is an intersection
                if (Intersect(p, extreme,start,end))
                {
                    count++;
                }
                    


            }

            // If there where an odd amount of intersection, the point lies within the polygon (so return true)
            return (count % 2 == 1);
        }



    }


}
