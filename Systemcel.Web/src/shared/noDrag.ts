import type React from "react";

export function preventNativeDrag(event: React.DragEvent<Element>) {
  event.preventDefault();
}
