﻿namespace SudokuBlazor.Models
{
    public class SvgText
    {
        public readonly double x;
        public readonly double y;
        public readonly double fontSize;
        public readonly string fontFamily;
        public readonly string color;
        public readonly string text;

        public SvgText(double x, double y, double fontSize, string fontFamily, string color, string text)
        {
            this.x = x;
            this.y = y;
            this.fontSize = fontSize;
            this.fontFamily = fontFamily;
            this.color = color;
            this.text = text;
        }
    }
}
