(function () {
  window.MsgBoxApi = window.MsgBoxApi || {};

  function formatErrorBody(body, res) {
    if (body == null) return res.statusText || "Request failed";
    if (typeof body === "string") return body;
    if (typeof body === "object") {
      if (body.detail) return String(body.detail);
      if (body.title) return String(body.title);
      if (body.message) return String(body.message);
      if (body.error) return String(body.error);
      if (body.errors && typeof body.errors === "object") {
        try {
          const parts = [];
          for (const k of Object.keys(body.errors)) {
            const v = body.errors[k];
            parts.push(k + ": " + (Array.isArray(v) ? v.join(", ") : String(v)));
          }
          if (parts.length) return parts.join("; ");
        } catch {
          /* ignore */
        }
      }
    }
    return res.statusText || "Request failed";
  }

  async function parseJsonResponse(res) {
    const text = await res.text();
    if (!text) return null;
    try {
      return JSON.parse(text);
    } catch {
      return text;
    }
  }

  async function handleResponse(res) {
    const body = await parseJsonResponse(res);
    if (!res.ok) throw new Error(formatErrorBody(body, res));
    return body;
  }

  MsgBoxApi.getJson = async function (url) {
    const res = await fetch(url, { headers: { Accept: "application/json" } });
    return handleResponse(res);
  };

  MsgBoxApi.putJson = async function (url, data) {
    const res = await fetch(url, {
      method: "PUT",
      headers: { "Content-Type": "application/json", Accept: "application/json" },
      body: JSON.stringify(data),
    });
    return handleResponse(res);
  };

  MsgBoxApi.postJson = async function (url, data) {
    const res = await fetch(url, {
      method: "POST",
      headers: { "Content-Type": "application/json", Accept: "application/json" },
      body: JSON.stringify(data),
    });
    return handleResponse(res);
  };

  MsgBoxApi.postForm = async function (url, formData) {
    const res = await fetch(url, {
      method: "POST",
      body: formData,
    });
    return handleResponse(res);
  };

  MsgBoxApi.putForm = async function (url, formData) {
    const res = await fetch(url, {
      method: "PUT",
      body: formData,
    });
    return handleResponse(res);
  };
})();
