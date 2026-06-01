import type React from "react";
import { preventNativeDrag } from "./noDrag";

type NoDragImageProps = React.ImgHTMLAttributes<HTMLImageElement>;

export function NoDragImage({ className, onDragStart, ...props }: NoDragImageProps) {
  const handleDragStart: React.DragEventHandler<HTMLImageElement> = (event) => {
    preventNativeDrag(event);
    onDragStart?.(event);
  };

  return (
    <img
      {...props}
      className={className ? `no-drag ${className}` : "no-drag"}
      draggable={false}
      onDragStart={handleDragStart}
    />
  );
}
