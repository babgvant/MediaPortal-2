﻿using SharpDX;

namespace MediaPortal.UI.SkinEngine
{
  public static class SharpDXExtensions
  {
    public static System.Drawing.RectangleF ToDrawingRectF(this RectangleF rectangleF)
    {
      return new System.Drawing.RectangleF(rectangleF.X, rectangleF.Y, rectangleF.Width, rectangleF.Height);
    }
    public static System.Drawing.SizeF ToDrawingSizeF(this Size2F size2F)
    {
      return new System.Drawing.SizeF(size2F.Width, size2F.Height);
    }
    public static System.Drawing.Size ToDrawingSize(this Size2 sizeF)
    {
      return new System.Drawing.Size(sizeF.Width, sizeF.Height);
    }
    public static Rectangle ToRect(this System.Drawing.Rectangle rectangleF)
    {
      return new Rectangle(rectangleF.X, rectangleF.Y, rectangleF.Width, rectangleF.Height);
    }
    public static Size2F ToSize2F(this System.Drawing.SizeF sizeF)
    {
      return new Size2F(sizeF.Width, sizeF.Height);
    }
    public static Size2 ToSize2(this System.Drawing.Size sizeF)
    {
      return new Size2(sizeF.Width, sizeF.Height);
    }
    public static Size2 ToSize(this Size2F sizeF)
    {
      return new Size2((int)sizeF.Width, (int)sizeF.Height);
    }
    public static Size2F ToSize2F(this Size2 sizeF)
    {
      return new Size2F(sizeF.Width, sizeF.Height);
    }
    public static bool IsEmpty(this Size2F sizeF)
    {
      return sizeF.Width == 0.0 && sizeF.Height == 0.0;
    }
    public static Rectangle CreateRectangle(Vector2 location, Size2 size)
    {
      return new Rectangle((int)location.X, (int)location.Y, size.Width, size.Height);
    }
    public static RectangleF CreateRectangleF(Vector2 location, Size2F size)
    {
      return new RectangleF(location.X, location.Y, size.Width, size.Height);
    }
  }
}
