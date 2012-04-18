using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace HeightmapCollision
{
    public class Button
    {
        Rectangle position;
        Texture2D idle;
        Texture2D pressed;
        Texture2D current;
        GameState transitionTo;


        bool selected;
        bool gamePadSelected = false;
        bool hovering;
        int buttonNum;
        double hover_start;
        private bool keyBoardSelected = false;

        public Button(Rectangle pos, Texture2D normal, Texture2D highlighted, GameState transition)
        {
            position = pos;
            idle = normal;
            pressed = highlighted;
            transitionTo = transition;
            current = idle;
        }

        public Button(Rectangle pos, Texture2D normal, Texture2D highlighted, GameState transition, int buttonNum)
        {
            position = pos;
            idle = normal;
            pressed = highlighted;
            transitionTo = transition;
            current = idle;
            this.buttonNum = buttonNum;
        }

        public GameState Update(GameTime gameTime, MouseState mouseState, Vector2 handPosition, 
            int curNum, PlayerIndex player, bool mouseFocus)
        {
            bool mouseHover = false;
            bool handHover = false;
            Vector2 mousePosition = new Vector2(mouseState.X, mouseState.Y);
            if (overlap(mousePosition, position))
            {
                mouseHover = true;
                if (current == idle)
                    current = pressed;
                if (mouseState.LeftButton == ButtonState.Pressed)
                    selected = true;

                if (selected && mouseState.LeftButton == ButtonState.Released)
                {
                    selected = false;
                    hovering = false;
                    return transitionTo;
                }
            }
            else
            {
                selected = false;
                if (!mouseFocus)
                {
                    if (curNum == buttonNum)
                    {
                        mouseHover = true;
                        current = pressed;
                        Keys selectKey;
                        if (player == PlayerIndex.One)
                        {
                            selectKey = Keys.RightShift;
                        }
                        else
                            selectKey = Keys.F;


                        if (!gamePadSelected && GamePad.GetState(player).Buttons.A == ButtonState.Pressed)
                        {
                            gamePadSelected = true;
                        }

                        if (gamePadSelected && GamePad.GetState(player).Buttons.A == ButtonState.Released)
                        {
                            gamePadSelected = false;
                            return transitionTo;
                        }

                        if (!keyBoardSelected && Keyboard.GetState().IsKeyDown(selectKey))
                        {
                            keyBoardSelected = true;
                        }

                        if (keyBoardSelected && Keyboard.GetState().IsKeyUp(selectKey))
                        {
                            keyBoardSelected = false;
                            return transitionTo;
                        }
                    }
                    else
                    {
                        current = idle;
                        selected = false;
                    }
                }
            }

            if (overlap(handPosition, position))
            {
                handHover = true;
                if (current == idle)
                    current = pressed;
                if (!hovering)
                {
                    hovering = true;
                    hover_start = gameTime.TotalGameTime.Seconds;
                }
                else
                {
                    if (gameTime.TotalGameTime.Seconds - hover_start >= 3)
                    {
                        hovering = false;
                        return transitionTo;
                    }
                }

            }
            else
            {
                hovering = false;
            }

            if (!mouseHover && !handHover && current != idle)
                current = idle;

            return GameState.NOCHANGE;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(current, position, Color.White);
        }

        bool overlap(Vector2 position, Rectangle rect)
        {
            if (position.X > rect.Left && position.X < rect.Right
                && position.Y > rect.Top && position.Y < rect.Bottom)
                return true;
            return false;
        }
    }
}
