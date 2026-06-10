import React from "react";
import { Camera, Check, ImagePlus, Loader2, RotateCcw, X } from "lucide-react";
import { muhasebeciProfilResmiYukle } from "./profilResmi";

const CROP_SIZE = 260;
const OUTPUT_SIZE = 640;

interface CropState {
  x: number;
  y: number;
  scale: number;
}

interface DisplaySize {
  width: number;
  height: number;
}

interface ProfilResmiYukleyiciProps {
  value: string;
  onChange: (url: string) => void;
  required?: boolean;
  disabled?: boolean;
  onBusyChange?: (busy: boolean) => void;
  onError?: (message: string) => void;
  className?: string;
}

export function ProfilResmiYukleyici({
  value,
  onChange,
  required,
  disabled,
  onBusyChange,
  onError,
  className
}: ProfilResmiYukleyiciProps) {
  const inputRef = React.useRef<HTMLInputElement | null>(null);
  const imageRef = React.useRef<HTMLImageElement | null>(null);
  const frameRef = React.useRef<HTMLDivElement | null>(null);
  const dragRef = React.useRef<{ startX: number; startY: number; cropX: number; cropY: number } | null>(null);
  const [seciliResimUrl, setSeciliResimUrl] = React.useState("");
  const [crop, setCrop] = React.useState<CropState>({ x: 0, y: 0, scale: 1 });
  const [displaySize, setDisplaySize] = React.useState<DisplaySize | null>(null);
  const [cropBoxSize, setCropBoxSize] = React.useState(CROP_SIZE);
  const [yukleniyor, setYukleniyor] = React.useState(false);

  React.useEffect(() => {
    return () => {
      if (seciliResimUrl) {
        URL.revokeObjectURL(seciliResimUrl);
      }
    };
  }, [seciliResimUrl]);

  function dosyaSecildi(event: React.ChangeEvent<HTMLInputElement>) {
    const file = event.currentTarget.files?.[0];
    if (!file)
      return;

    if (!file.type.startsWith("image/")) {
      onError?.("Profil resmi JPG, PNG veya WEBP olmalıdır.");
      event.currentTarget.value = "";
      return;
    }

    if (seciliResimUrl) {
      URL.revokeObjectURL(seciliResimUrl);
    }

    setDisplaySize(null);
    setCrop({ x: 0, y: 0, scale: 1 });
    setSeciliResimUrl(URL.createObjectURL(file));
  }

  function resimYuklendi(event: React.SyntheticEvent<HTMLImageElement>) {
    const image = event.currentTarget;
    const nextCropBoxSize = frameRef.current?.clientWidth || CROP_SIZE;
    const ratio = image.naturalWidth / image.naturalHeight;
    const nextSize = ratio >= 1
      ? { width: nextCropBoxSize * ratio, height: nextCropBoxSize }
      : { width: nextCropBoxSize, height: nextCropBoxSize / ratio };

    setCropBoxSize(nextCropBoxSize);
    setDisplaySize(nextSize);
    setCrop({ x: 0, y: 0, scale: 1 });
  }

  function suruklemeBasladi(event: React.PointerEvent<HTMLDivElement>) {
    if (!displaySize)
      return;

    event.currentTarget.setPointerCapture(event.pointerId);
    dragRef.current = {
      startX: event.clientX,
      startY: event.clientY,
      cropX: crop.x,
      cropY: crop.y
    };
  }

  function surukleniyor(event: React.PointerEvent<HTMLDivElement>) {
    if (!dragRef.current || !displaySize)
      return;

    const next = {
      ...crop,
      x: dragRef.current.cropX + event.clientX - dragRef.current.startX,
      y: dragRef.current.cropY + event.clientY - dragRef.current.startY
    };
    setCrop(clampCrop(next, displaySize, cropBoxSize));
  }

  function suruklemeBitti(event: React.PointerEvent<HTMLDivElement>) {
    dragRef.current = null;
    if (event.currentTarget.hasPointerCapture(event.pointerId)) {
      event.currentTarget.releasePointerCapture(event.pointerId);
    }
  }

  function zoomDegisti(value: number) {
    if (!displaySize) {
      setCrop((current) => ({ ...current, scale: value }));
      return;
    }

    setCrop((current) => clampCrop({ ...current, scale: value }, displaySize, cropBoxSize));
  }

  function zoomTekerlek(event: React.WheelEvent<HTMLDivElement>) {
    if (!displaySize)
      return;

    event.preventDefault();
    const nextScale = clamp(crop.scale + (event.deltaY > 0 ? -0.08 : 0.08), 1, 3);
    setCrop((current) => clampCrop({ ...current, scale: nextScale }, displaySize, cropBoxSize));
  }

  function kirpmayiSifirla() {
    setCrop({ x: 0, y: 0, scale: 1 });
  }

  async function kirpVeYukle() {
    const image = imageRef.current;
    if (!image || !displaySize)
      return;

    setYukleniyor(true);
    onBusyChange?.(true);
    try {
      const file = await croppedFileFromImage(image, displaySize, crop, cropBoxSize);
      const url = await muhasebeciProfilResmiYukle(file);
      onChange(url);
      kapat();
    } catch (error) {
      onError?.(error instanceof Error ? error.message : "Profil resmi yüklenemedi.");
    } finally {
      setYukleniyor(false);
      onBusyChange?.(false);
    }
  }

  function kapat() {
    if (seciliResimUrl) {
      URL.revokeObjectURL(seciliResimUrl);
    }
    setSeciliResimUrl("");
    setDisplaySize(null);
    setCropBoxSize(CROP_SIZE);
    setCrop({ x: 0, y: 0, scale: 1 });
    if (inputRef.current) {
      inputRef.current.value = "";
    }
  }

  const rootClassName = className ? `profile-image-uploader ${className}` : "profile-image-uploader";

  return (
    <div className={rootClassName}>
      <span className="profile-image-uploader__label">
        <Camera size={17} />
        Profil resmi
      </span>
      <div className="profile-image-uploader__body">
        <div className="profile-image-uploader__preview">
          {value ? <img src={value} alt="" /> : <Camera size={24} />}
        </div>
        <div className="profile-image-uploader__actions">
          <button type="button" onClick={() => inputRef.current?.click()} disabled={disabled || yukleniyor}>
            {yukleniyor ? <Loader2 size={15} className="spin" /> : <ImagePlus size={15} />}
            <span>{value ? "Resmi değiştir" : "Resim seç"}</span>
          </button>
          <small>{required && !value ? "JPG, PNG veya WEBP" : "Profil resmi hazır"}</small>
        </div>
      </div>
      <input
        ref={inputRef}
        className="profile-image-uploader__file"
        type="file"
        accept="image/png,image/jpeg,image/webp"
        onChange={dosyaSecildi}
        disabled={disabled || yukleniyor}
      />

      {seciliResimUrl ? (
        <div className="profile-cropper" role="dialog" aria-modal="true" aria-label="Profil resmini kırp">
          <section className="profile-cropper__panel">
            <header>
              <div>
                <span>Profil resmi</span>
                <h2>Kırp ve önizle</h2>
              </div>
              <button type="button" onClick={kapat} aria-label="Kapat">
                <X size={18} />
              </button>
            </header>
            <div
              ref={frameRef}
              className="profile-cropper__frame"
              onPointerDown={suruklemeBasladi}
              onPointerMove={surukleniyor}
              onPointerUp={suruklemeBitti}
              onPointerCancel={suruklemeBitti}
              onWheel={zoomTekerlek}
            >
              <img
                ref={imageRef}
                src={seciliResimUrl}
                alt=""
                onLoad={resimYuklendi}
                draggable={false}
                style={{
                  width: `${displaySize?.width ?? CROP_SIZE}px`,
                  height: `${displaySize?.height ?? CROP_SIZE}px`,
                  transform: `translate(-50%, -50%) translate(${crop.x}px, ${crop.y}px) scale(${crop.scale})`
                }}
              />
            </div>
            <label className="profile-cropper__zoom">
              <span>Yakınlaştır</span>
              <input
                type="range"
                min="1"
                max="3"
                step="0.01"
                value={crop.scale}
                onChange={(event) => zoomDegisti(Number(event.target.value))}
              />
            </label>
            <div className="profile-cropper__actions">
              <button type="button" onClick={kirpmayiSifirla} disabled={yukleniyor}>
                <RotateCcw size={15} />
                <span>Sıfırla</span>
              </button>
              <button type="button" className="profile-cropper__primary" onClick={kirpVeYukle} disabled={yukleniyor || !displaySize}>
                {yukleniyor ? <Loader2 size={16} className="spin" /> : <Check size={16} />}
                <span>Kırp ve yükle</span>
              </button>
            </div>
          </section>
        </div>
      ) : null}
    </div>
  );
}

async function croppedFileFromImage(image: HTMLImageElement, displaySize: DisplaySize, crop: CropState, cropBoxSize: number) {
  const canvas = document.createElement("canvas");
  canvas.width = OUTPUT_SIZE;
  canvas.height = OUTPUT_SIZE;
  const context = canvas.getContext("2d");
  if (!context) {
    throw new Error("Profil resmi kırpılamadı.");
  }

  const scaledWidth = displaySize.width * crop.scale;
  const scaledHeight = displaySize.height * crop.scale;
  const left = cropBoxSize / 2 - scaledWidth / 2 + crop.x;
  const top = cropBoxSize / 2 - scaledHeight / 2 + crop.y;
  const sourceX = clamp((-left / scaledWidth) * image.naturalWidth, 0, image.naturalWidth);
  const sourceY = clamp((-top / scaledHeight) * image.naturalHeight, 0, image.naturalHeight);
  const sourceWidth = clamp((cropBoxSize / scaledWidth) * image.naturalWidth, 1, image.naturalWidth - sourceX);
  const sourceHeight = clamp((cropBoxSize / scaledHeight) * image.naturalHeight, 1, image.naturalHeight - sourceY);

  context.drawImage(image, sourceX, sourceY, sourceWidth, sourceHeight, 0, 0, OUTPUT_SIZE, OUTPUT_SIZE);
  const blob = await canvasToBlob(canvas);
  return new File([blob], "profil-resmi.jpg", { type: "image/jpeg" });
}

function canvasToBlob(canvas: HTMLCanvasElement) {
  return new Promise<Blob>((resolve, reject) => {
    canvas.toBlob((blob) => {
      if (blob) {
        resolve(blob);
      } else {
        reject(new Error("Profil resmi oluşturulamadı."));
      }
    }, "image/jpeg", 0.92);
  });
}

function clampCrop(crop: CropState, displaySize: DisplaySize, cropBoxSize: number) {
  const scaledWidth = displaySize.width * crop.scale;
  const scaledHeight = displaySize.height * crop.scale;
  const maxX = Math.max(0, (scaledWidth - cropBoxSize) / 2);
  const maxY = Math.max(0, (scaledHeight - cropBoxSize) / 2);

  return {
    scale: clamp(crop.scale, 1, 3),
    x: clamp(crop.x, -maxX, maxX),
    y: clamp(crop.y, -maxY, maxY)
  };
}

function clamp(value: number, min: number, max: number) {
  return Math.min(max, Math.max(min, value));
}
