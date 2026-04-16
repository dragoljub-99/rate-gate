const API_BASE_URL = "http://localhost:5294";

function $(selector, root = document) {
  return root.querySelector(selector);
}

function createEl(tagName, className = "", text = "") {
  const el = document.createElement(tagName);
  if (className) el.className = className;
  if (text) el.textContent = text;
  return el;
}

function clearEl(el) {
  while (el.firstChild) el.removeChild(el.firstChild);
}

function setStatus(message, kind = "muted") {
  const status = $("#statusLine");
  status.textContent = message;
  status.classList.remove("good", "bad", "warn", "muted");
  status.classList.add(kind);
}

async function apiRequest(method, path, body = null) {
  const url = API_BASE_URL + path;

  const options = {
    method,
    headers: {}
  };

  if (body !== null) {
    options.headers["Content-Type"] = "application/json";
    options.body = JSON.stringify(body);
  }

  let response;
  try {
    response = await fetch(url, options);
  } catch (networkError) {
    throw new Error(`Network error calling ${method} ${url}: ${networkError.message}`);
  }

  const text = await response.text();
  const hasBody = text && text.trim().length > 0;
  let json = null;

  if (hasBody) {
    try {
      json = JSON.parse(text);
    } catch {
      json = { raw: text };
    }
  }

  if (!response.ok) {
    const details = hasBody ? JSON.stringify(json) : "(no body)";
    throw new Error(`HTTP ${response.status} calling ${method} ${url}: ${details}`);
  }

  return json;
}

const api = {
  get: (path) => apiRequest("GET", path),
  post: (path, body) => apiRequest("POST", path, body),
  put: (path, body) => apiRequest("PUT", path, body),
  del: (path) => apiRequest("DELETE", path)
};

async function renderCheckPage() {
  const root = $("#page-check");
  clearEl(root);

  const card = createEl("div", "card");
  card.appendChild(createEl("h2", "", "Try /check (Decision API)"));
  card.appendChild(createEl("div", "muted", "Use this to demonstrate allow/deny behavior from policies and algorithms."));
  root.appendChild(card);

  const formCard = createEl("div", "card");
  formCard.appendChild(createEl("h2", "", "Request"));

  const form = document.createElement("form");
  form.innerHTML = `
    <div class="form-row">
      <div class="field">
        <label>ApiKey</label>
        <input name="apiKey" value="demo-key-1" required />
      </div>
      <div class="field">
        <label>Endpoint</label>
        <input name="endpoint" value="/demo" required />
      </div>
    </div>

    <div class="field">
      <label>Cost (optional, defaults to 1)</label>
      <input name="cost" type="number" min="1" placeholder="1" />
    </div>

    <div class="actions">
      <button class="btn primary" type="submit">POST /check</button>
      <button class="btn" type="button" id="btnDemoToken">Use /demo (TokenBucket via wildcard)</button>
      <button class="btn" type="button" id="btnDemoSliding">Use /sliding-demo (SlidingWindow exact)</button>
    </div>
  `;

  const resultCard = createEl("div", "card");
  resultCard.appendChild(createEl("h2", "", "Response"));
  const pre = document.createElement("pre");
  pre.style.whiteSpace = "pre-wrap";
  pre.style.margin = "0";
  pre.style.color = "var(--text)";
  pre.textContent = "(no response yet)";
  resultCard.appendChild(pre);

  form.addEventListener("submit", async (e) => {
    e.preventDefault();
    await handleCheck(form, pre);
  });

  form.querySelector("#btnDemoToken").addEventListener("click", () => {
    form.endpoint.value = "/demo";
  });

  form.querySelector("#btnDemoSliding").addEventListener("click", () => {
    form.endpoint.value = "/sliding-demo";
  });

  formCard.appendChild(form);
  root.appendChild(formCard);
  root.appendChild(resultCard);
}

async function handleCheck(form, outputPre) {
  const apiKey = form.apiKey.value.trim();
  const endpoint = form.endpoint.value.trim();
  const costRaw = form.cost.value.trim();
  const cost = costRaw ? Number(costRaw) : null;

  if (!apiKey) return setStatus("ApiKey is required.", "warn");
  if (!endpoint) return setStatus("Endpoint is required.", "warn");
  if (cost !== null && (!cost || cost <= 0)) return setStatus("Cost must be positive if provided.", "warn");

  try {
    setStatus("Calling POST /check ...", "muted");
    const body = { apiKey, endpoint };
    if (cost !== null) body.cost = cost;

    const result = await api.post("/check", body);
    setStatus(`Decision: allow=${result.allow} reason=${result.reason}`, result.allow ? "good" : "warn");

    outputPre.textContent = JSON.stringify(result, null, 2);
  } catch (err) {
    setStatus(err.message, "bad");
    outputPre.textContent = err.message;
  }
}

async function wireTopbarButtons() {
  $("#btnPing").addEventListener("click", async () => {
    try {
      setStatus("Calling GET /health ...", "muted");
      const res = await api.get("/health");
      setStatus(`Health OK: ${res.status}`, "good");
    } catch (err) {
      setStatus(err.message, "bad");
    }
  });

  $("#btnDbPing").addEventListener("click", async () => {
    try {
      setStatus("Calling GET /health/db ...", "muted");
      const res = await api.get("/health/db");
      setStatus(`DB Health: canConnect=${res.canConnect}`, res.canConnect ? "good" : "warn");
    } catch (err) {
      setStatus(err.message, "bad");
    }
  });
}

async function startApp() {
  await wireTopbarButtons();
  await renderCheckPage();
}

startApp().catch((err) => {
  setStatus(`Startup error: ${err.message}`, "bad");
});
