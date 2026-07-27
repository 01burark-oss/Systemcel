import { getAuthToken } from "../auth/authToken";

interface AuthenticatedFileOptions {
  fileName?: string;
  contentType?: string;
}

export async function openAuthenticatedFile(
  url: string,
  { fileName = "systemcel-dosya", contentType = "" }: AuthenticatedFileOptions = {}
) {
  const canPreview = contentType === "application/pdf" || contentType.startsWith("image/") || contentType.startsWith("text/");
  const previewWindow = canPreview ? window.open("about:blank", "_blank") : null;
  if (previewWindow) {
    previewWindow.opener = null;
  }

  try {
    const blob = await fetchAuthenticatedFileBlob(url);
    const objectUrl = URL.createObjectURL(blob);

    if (canPreview && previewWindow) {
      previewWindow.location.replace(objectUrl);
    } else {
      previewWindow?.close();
      const anchor = document.createElement("a");
      anchor.href = objectUrl;
      anchor.download = fileName;
      anchor.style.display = "none";
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
    }

    window.setTimeout(() => URL.revokeObjectURL(objectUrl), 60_000);
  } catch (error) {
    previewWindow?.close();
    throw error;
  }
}

export async function fetchAuthenticatedFileBlob(url: string) {
  const token = await getAuthToken();
  const headers = new Headers();
  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  const response = await fetch(url, { headers });
  if (!response.ok) {
    const payload = await readErrorPayload(response);
    throw new Error(payload || `Dosya açılamadı (HTTP ${response.status}).`);
  }

  return response.blob();
}

async function readErrorPayload(response: Response) {
  try {
    const text = await response.text();
    if (!text) {
      return "";
    }

    const payload = JSON.parse(text) as { mesaj?: string; detail?: string };
    return payload.mesaj || payload.detail || "";
  } catch {
    return "";
  }
}
