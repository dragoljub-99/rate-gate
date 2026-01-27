const API_BASE_URL = "http://localhost:5294";

const state = {
  currentPage: "users",
  selectedUserId: null,
  selectedPolicyId: null,
  lastWindowSeconds: 3600
};

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

function fmtUtc(iso) {
  if (!iso) return "";
  return String(iso).replace("T", " ");
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

function renderTable(container, columns, rows) {
  clearEl(container);

  const wrap = createEl("div", "table-wrap");
  const table = document.createElement("table");

  const thead = document.createElement("thead");
  const headRow = document.createElement("tr");
  for (const col of columns) {
    const th = document.createElement("th");
    th.textContent = col.title;
    headRow.appendChild(th);
  }
  thead.appendChild(headRow);

  const tbody = document.createElement("tbody");
  for (const row of rows) {
    const tr = document.createElement("tr");
    for (const col of columns) {
      const td = document.createElement("td");
      if (col.render) {
        td.appendChild(col.render(row));
      } else {
        const value = row[col.key];
        td.textContent = value === null || value === undefined ? "" : String(value);
      }
      tr.appendChild(td);
    }
    tbody.appendChild(tr);
  }

  table.appendChild(thead);
  table.appendChild(tbody);
  wrap.appendChild(table);
  container.appendChild(wrap);
}

function showPage(pageName) {
  state.currentPage = pageName;

  const buttons = document.querySelectorAll(".nav-item");
  for (const btn of buttons) {
    btn.classList.toggle("is-active", btn.dataset.page === pageName);
  }

  const pageIds = ["users", "apikeys", "policies", "metrics", "check"];
  for (const id of pageIds) {
    const section = $(`#page-${id}`);
    section.classList.toggle("hidden", id !== pageName);
  }

  if (pageName === "users") renderUsersPage();
  if (pageName === "apikeys") renderApiKeysPage();
  if (pageName === "policies") renderPoliciesPage();
  if (pageName === "metrics") renderMetricsPage();
  if (pageName === "check") renderCheckPage();
}

async function renderUsersPage() {
  const root = $("#page-users");
  clearEl(root);

  root.appendChild(renderUsersHeaderCard());
  root.appendChild(renderCreateUserCard());

  const listCard = createEl("div", "card");
  listCard.appendChild(createEl("h2", "", "Users"));
  listCard.appendChild(createEl("div", "muted small", "This data comes from GET /admin/users."));
  const tableHost = createEl("div");
  listCard.appendChild(tableHost);
  root.appendChild(listCard);

  await loadUsersInto(tableHost);
}

function renderUsersHeaderCard() {
  const card = createEl("div", "card");
  card.appendChild(createEl("h2", "", "Users & Tenants"));
  card.appendChild(createEl("div", "muted", "Create tenants, view their keys and policies, and drill into details."));
  return card;
}

function renderCreateUserCard() {
  const card = createEl("div", "card");
  card.appendChild(createEl("h2", "", "Create user"));

  const form = document.createElement("form");
  form.innerHTML = `
    <div class="form-row">
      <div class="field">
        <label>Name (required)</label>
        <input name="name" placeholder="e.g. Demo Tenant 2" required />
      </div>
      <div class="field">
        <label>Email (optional)</label>
        <input name="email" placeholder="e.g. user@example.com" />
      </div>
    </div>

    <div class="field">
      <label>Plan (optional)</label>
      <input name="plan" placeholder="e.g. free / pro / enterprise" />
    </div>

    <div class="actions">
      <button class="btn primary" type="submit">Create user</button>
      <button class="btn" type="button" id="btnRefreshUsers">Refresh list</button>
    </div>
  `;

  form.addEventListener("submit", async (e) => {
    e.preventDefault();
    await handleCreateUser(form);
  });

  form.querySelector("#btnRefreshUsers").addEventListener("click", () => {
    showPage("users");
  });

  card.appendChild(form);
  return card;
}

async function handleCreateUser(form) {
  const name = form.name.value.trim();
  const email = form.email.value.trim();
  const plan = form.plan.value.trim();

  if (!name) {
    setStatus("Name is required.", "warn");
    return;
  }

  try {
    setStatus("Creating user...", "muted");
    const body = {
      name,
      email: email || null,
      plan: plan || null
    };
    const created = await api.post("/admin/users", body);
    setStatus(`Created user id=${created.id} (${created.name}).`, "good");

    form.reset();

    showPage("users");
  } catch (err) {
    setStatus(err.message, "bad");
  }
}

async function loadUsersInto(container) {
  try {
    setStatus("Loading users...", "muted");
    const users = await api.get("/admin/users");

    const columns = [
      { key: "id", title: "Id" },
      { key: "name", title: "Name" },
      { key: "email", title: "Email" },
      { key: "plan", title: "Plan" },
      { key: "apiKeysCount", title: "Keys" },
      { key: "policiesCount", title: "Policies" },
      {
        title: "Actions",
        render: (row) => {
          const wrap = createEl("div", "");
          const btn = createEl("button", "btn small", "View");
          btn.addEventListener("click", () => {
            state.selectedUserId = row.id;
            showUserDetailsModal(row.id);
          });
          wrap.appendChild(btn);
          return wrap;
        }
      }
    ];

    renderTable(container, columns, users);

    setStatus(`Loaded ${users.length} user(s).`, "good");
  } catch (err) {
    setStatus(err.message, "bad");
    clearEl(container);
    container.appendChild(createEl("div", "muted", "Failed to load users."));
  }
}

async function showUserDetailsModal(userId) {
  const overlay = createEl("div");
  overlay.style.position = "fixed";
  overlay.style.inset = "0";
  overlay.style.background = "rgba(0,0,0,0.55)";
  overlay.style.display = "grid";
  overlay.style.placeItems = "center";
  overlay.style.padding = "16px";
  overlay.style.zIndex = "9999";

  const modal = createEl("div", "page");
  modal.style.width = "min(1100px, 100%)";
  modal.style.maxHeight = "85vh";
  modal.style.overflow = "auto";

  const header = createEl("div", "actions");
  const closeBtn = createEl("button", "btn", "Close");
  closeBtn.addEventListener("click", () => overlay.remove());
  header.appendChild(closeBtn);

  modal.appendChild(createEl("h2", "", `User details (id=${userId})`));
  modal.appendChild(header);

  const content = createEl("div");
  modal.appendChild(content);

  overlay.appendChild(modal);
  document.body.appendChild(overlay);

  try {
    setStatus(`Loading user ${userId}...`, "muted");
    const user = await api.get(`/admin/users/${userId}`);

    content.appendChild(renderUserDetails(user));
    setStatus(`Loaded user ${userId}.`, "good");
  } catch (err) {
    setStatus(err.message, "bad");
    content.appendChild(createEl("div", "muted", "Failed to load user details."));
  }
}

function renderUserDetails(user) {
  const wrap = createEl("div");

  const summary = createEl("div", "card");
  summary.appendChild(createEl("h2", "", "Summary"));
  summary.appendChild(createEl("div", "small muted", `CreatedAtUtc: ${fmtUtc(user.createdAtUtc)}`));
  summary.appendChild(createEl("div", "", `Name: ${user.name}`));
  summary.appendChild(createEl("div", "", `Email: ${user.email || ""}`));
  summary.appendChild(createEl("div", "", `Plan: ${user.plan || ""}`));
  wrap.appendChild(summary);

  const keysCard = createEl("div", "card");
  keysCard.appendChild(createEl("h2", "", "API Keys"));
  const keysHost = createEl("div");
  keysCard.appendChild(keysHost);

  const keys = user.apiKeys || [];
  renderTable(keysHost, [
    { key: "id", title: "Id" },
    { key: "key", title: "Key" },
    {
      title: "Active",
      render: (row) => {
        const b = createEl("span", `badge ${row.isActive ? "good" : "bad"}`, row.isActive ? "Active" : "Inactive");
        return b;
      }
    },
    { title: "Created", render: (row) => createEl("span", "", fmtUtc(row.createdAtUtc)) },
    { title: "Last used", render: (row) => createEl("span", "", fmtUtc(row.lastUsedAtUtc)) }
  ], keys);

  wrap.appendChild(keysCard);

  const policiesCard = createEl("div", "card");
  policiesCard.appendChild(createEl("h2", "", "Policies"));
  const polHost = createEl("div");
  policiesCard.appendChild(polHost);

  const policies = user.policies || [];
  renderTable(polHost, [
    { key: "id", title: "Id" },
    { key: "name", title: "Name" },
    { key: "endpointPattern", title: "EndpointPattern" },
    { key: "algorithm", title: "Algorithm" },
    { key: "limit", title: "Limit" },
    { key: "windowInSeconds", title: "Window(s)" }
  ], policies);

  wrap.appendChild(policiesCard);

  return wrap;
}

async function renderApiKeysPage() {
  const root = $("#page-apikeys");
  clearEl(root);

  const intro = createEl("div", "card");
  intro.appendChild(createEl("h2", "", "API Keys"));
  intro.appendChild(createEl("div", "muted", "Create keys, look up keys by id, and activate/deactivate them."));
  root.appendChild(intro);

  root.appendChild(renderCreateApiKeyCard());
  root.appendChild(renderLookupApiKeyCard());
}

function renderCreateApiKeyCard() {
  const card = createEl("div", "card");
  card.appendChild(createEl("h2", "", "Create API key"));

  const form = document.createElement("form");
  form.innerHTML = `
    <div class="form-row">
      <div class="field">
        <label>UserId (required)</label>
        <input name="userId" type="number" min="1" placeholder="e.g. 1" required />
      </div>
      <div class="field">
        <label>Key (optional)</label>
        <input name="key" placeholder="Leave blank to auto-generate" />
      </div>
    </div>

    <div class="field">
      <label>IsActive (optional)</label>
      <select name="isActive">
        <option value="">(default true)</option>
        <option value="true">true</option>
        <option value="false">false</option>
      </select>
    </div>

    <div class="actions">
      <button class="btn primary" type="submit">Create key</button>
    </div>

    <div class="small muted" id="createKeyResult"></div>
  `;

  form.addEventListener("submit", async (e) => {
    e.preventDefault();
    await handleCreateApiKey(form);
  });

  card.appendChild(form);
  return card;
}

async function handleCreateApiKey(form) {
  const userId = Number(form.userId.value);
  const key = form.key.value.trim();
  const isActiveRaw = form.isActive.value;

  const resultLine = form.querySelector("#createKeyResult");
  resultLine.textContent = "";

  if (!userId || userId <= 0) {
    setStatus("UserId must be a positive integer.", "warn");
    return;
  }

  const isActive =
    isActiveRaw === "" ? null :
    isActiveRaw === "true";

  try {
    setStatus("Creating API key...", "muted");
    const created = await api.post("/admin/apikeys", {
      userId,
      key: key || null,
      isActive
    });

    setStatus(`Created API key id=${created.id} for userId=${created.userId}.`, "good");
    resultLine.textContent = `Key value: ${created.key}`;

    form.reset();
  } catch (err) {
    setStatus(err.message, "bad");
  }
}

function renderLookupApiKeyCard() {
  const card = createEl("div", "card");
  card.appendChild(createEl("h2", "", "Lookup / activate / deactivate"));

  const form = document.createElement("form");
  form.innerHTML = `
    <div class="form-row">
      <div class="field">
        <label>ApiKey Id</label>
        <input name="id" type="number" min="1" placeholder="e.g. 1" required />
      </div>
      <div class="field">
        <label>&nbsp;</label>
        <button class="btn primary" type="submit">Load key</button>
      </div>
    </div>

    <div class="actions">
      <button class="btn good" type="button" id="btnActivate">Activate</button>
      <button class="btn danger" type="button" id="btnDeactivate">Deactivate</button>
    </div>

    <div class="small muted" id="lookupResult"></div>
  `;

  const lookupResult = form.querySelector("#lookupResult");
  let loadedKeyId = null;

  form.addEventListener("submit", async (e) => {
    e.preventDefault();
    const id = Number(form.id.value);
    loadedKeyId = await loadApiKeyById(id, lookupResult);
  });

  form.querySelector("#btnActivate").addEventListener("click", async () => {
    if (!loadedKeyId) return setStatus("Load a key first.", "warn");
    await toggleApiKey(loadedKeyId, true, lookupResult);
  });

  form.querySelector("#btnDeactivate").addEventListener("click", async () => {
    if (!loadedKeyId) return setStatus("Load a key first.", "warn");
    await toggleApiKey(loadedKeyId, false, lookupResult);
  });

  card.appendChild(form);
  return card;
}

async function loadApiKeyById(id, outEl) {
  if (!id || id <= 0) {
    setStatus("ApiKey id must be a positive integer.", "warn");
    return null;
  }

  try {
    setStatus(`Loading API key ${id}...`, "muted");
    const key = await api.get(`/admin/apikeys/${id}`);
    setStatus(`Loaded API key ${id}.`, "good");
    outEl.textContent = `id=${key.id} userId=${key.userId} active=${key.isActive} key=${key.key}`;
    return key.id;
  } catch (err) {
    setStatus(err.message, "bad");
    outEl.textContent = "";
    return null;
  }
}

async function toggleApiKey(id, activate, outEl) {
  try {
    setStatus(`${activate ? "Activating" : "Deactivating"} key ${id}...`, "muted");
    const updated = await api.post(`/admin/apikeys/${id}/${activate ? "activate" : "deactivate"}`, {});
    setStatus(`Key ${id} is now ${updated.isActive ? "active" : "inactive"}.`, "good");
    outEl.textContent = `id=${updated.id} userId=${updated.userId} active=${updated.isActive} key=${updated.key}`;
  } catch (err) {
    setStatus(err.message, "bad");
  }
}

async function renderPoliciesPage() {
  const root = $("#page-policies");
  clearEl(root);

  const intro = createEl("div", "card");
  intro.appendChild(createEl("h2", "", "Policies"));
  intro.appendChild(createEl("div", "muted", "Create, update, delete policies. Policies control algorithm + limit/window."));
  root.appendChild(intro);

  root.appendChild(renderCreatePolicyCard());

  const listCard = createEl("div", "card");
  listCard.appendChild(createEl("h2", "", "All policies"));
  listCard.appendChild(createEl("div", "muted small", "Data from GET /admin/policies."));
  const host = createEl("div");
  listCard.appendChild(host);

  const actions = createEl("div", "actions");
  const btnRefresh = createEl("button", "btn", "Refresh");
  btnRefresh.addEventListener("click", () => showPage("policies"));
  actions.appendChild(btnRefresh);
  listCard.appendChild(actions);

  root.appendChild(listCard);

  await loadPoliciesInto(host);
}

function renderCreatePolicyCard() {
  const card = createEl("div", "card");
  card.appendChild(createEl("h2", "", "Create policy"));

  const form = document.createElement("form");
  form.innerHTML = `
    <div class="form-row">
      <div class="field">
        <label>UserId (required)</label>
        <input name="userId" type="number" min="1" placeholder="e.g. 1" required />
      </div>
      <div class="field">
        <label>Name (required)</label>
        <input name="name" placeholder="e.g. Orders limit" required />
      </div>
    </div>

    <div class="field">
      <label>EndpointPattern (required)</label>
      <input name="endpointPattern" placeholder='Examples: "*", "/sliding-demo", "/api/orders/*"' required />
    </div>

    <div class="form-row">
      <div class="field">
        <label>Algorithm</label>
        <select name="algorithm">
          <option value="TokenBucket">TokenBucket</option>
          <option value="SlidingWindowLog">SlidingWindowLog</option>
        </select>
      </div>
      <div class="field">
        <label>BurstLimit (optional)</label>
        <input name="burstLimit" type="number" min="1" placeholder="e.g. 5 (TokenBucket only)" />
      </div>
    </div>

    <div class="form-row">
      <div class="field">
        <label>Limit (required)</label>
        <input name="limit" type="number" min="1" value="10" required />
      </div>
      <div class="field">
        <label>WindowInSeconds (required)</label>
        <input name="windowInSeconds" type="number" min="1" value="10" required />
      </div>
    </div>

    <div class="actions">
      <button class="btn primary" type="submit">Create policy</button>
    </div>
  `;

  form.addEventListener("submit", async (e) => {
    e.preventDefault();
    await handleCreatePolicy(form);
  });

  card.appendChild(form);
  return card;
}

async function handleCreatePolicy(form) {
  const userId = Number(form.userId.value);
  const name = form.name.value.trim();
  const endpointPattern = form.endpointPattern.value.trim();
  const algorithm = form.algorithm.value;
  const limit = Number(form.limit.value);
  const windowInSeconds = Number(form.windowInSeconds.value);
  const burstLimitRaw = form.burstLimit.value.trim();
  const burstLimit = burstLimitRaw ? Number(burstLimitRaw) : null;

  if (!userId || userId <= 0) return setStatus("UserId must be positive.", "warn");
  if (!name) return setStatus("Name is required.", "warn");
  if (!endpointPattern) return setStatus("EndpointPattern is required.", "warn");
  if (!limit || limit <= 0) return setStatus("Limit must be positive.", "warn");
  if (!windowInSeconds || windowInSeconds <= 0) return setStatus("WindowInSeconds must be positive.", "warn");

  try {
    setStatus("Creating policy...", "muted");
    const created = await api.post("/admin/policies", {
      userId,
      name,
      endpointPattern,
      algorithm,
      limit,
      windowInSeconds,
      burstLimit
    });

    setStatus(`Created policy id=${created.id}.`, "good");
    form.reset();
    showPage("policies");
  } catch (err) {
    setStatus(err.message, "bad");
  }
}

async function loadPoliciesInto(container) {
  try {
    setStatus("Loading policies...", "muted");
    const policies = await api.get("/admin/policies");

    const columns = [
      { key: "id", title: "Id" },
      { key: "userId", title: "UserId" },
      { key: "name", title: "Name" },
      { key: "endpointPattern", title: "EndpointPattern" },
      { key: "algorithm", title: "Algorithm" },
      { key: "limit", title: "Limit" },
      { key: "windowInSeconds", title: "Window(s)" },
      {
        title: "Actions",
        render: (row) => {
          const wrap = createEl("div", "actions");
          wrap.style.margin = "0";

          const btnEdit = createEl("button", "btn small", "Edit");
          btnEdit.addEventListener("click", () => showEditPolicyModal(row));

          const btnDel = createEl("button", "btn small danger", "Delete");
          btnDel.addEventListener("click", () => handleDeletePolicy(row.id));

          wrap.appendChild(btnEdit);
          wrap.appendChild(btnDel);
          return wrap;
        }
      }
    ];

    renderTable(container, columns, policies);
    setStatus(`Loaded ${policies.length} policy(ies).`, "good");
  } catch (err) {
    setStatus(err.message, "bad");
    clearEl(container);
    container.appendChild(createEl("div", "muted", "Failed to load policies."));
  }
}

async function handleDeletePolicy(policyId) {
  const ok = confirm(`Delete policy id=${policyId}? This cannot be undone.`);
  if (!ok) return;

  try {
    setStatus(`Deleting policy ${policyId}...`, "muted");
    await api.del(`/admin/policies/${policyId}`);
    setStatus(`Deleted policy ${policyId}.`, "good");
    showPage("policies");
  } catch (err) {
    setStatus(err.message, "bad");
  }
}

function showEditPolicyModal(policy) {
  const overlay = createEl("div");
  overlay.style.position = "fixed";
  overlay.style.inset = "0";
  overlay.style.background = "rgba(0,0,0,0.55)";
  overlay.style.display = "grid";
  overlay.style.placeItems = "center";
  overlay.style.padding = "16px";
  overlay.style.zIndex = "9999";

  const modal = createEl("div", "page");
  modal.style.width = "min(900px, 100%)";
  modal.style.maxHeight = "85vh";
  modal.style.overflow = "auto";

  modal.appendChild(createEl("h2", "", `Edit policy (id=${policy.id})`));

  const form = document.createElement("form");
  form.innerHTML = `
    <div class="field">
      <label>Name</label>
      <input name="name" value="${escapeHtml(policy.name)}" required />
    </div>

    <div class="field">
      <label>EndpointPattern</label>
      <input name="endpointPattern" value="${escapeHtml(policy.endpointPattern)}" required />
    </div>

    <div class="form-row">
      <div class="field">
        <label>Algorithm</label>
        <select name="algorithm">
          <option value="TokenBucket" ${policy.algorithm === "TokenBucket" ? "selected" : ""}>TokenBucket</option>
          <option value="SlidingWindowLog" ${policy.algorithm === "SlidingWindowLog" ? "selected" : ""}>SlidingWindowLog</option>
        </select>
      </div>

      <div class="field">
        <label>BurstLimit (optional)</label>
        <input name="burstLimit" type="number" min="1" value="${policy.burstLimit ?? ""}" />
      </div>
    </div>

    <div class="form-row">
      <div class="field">
        <label>Limit</label>
        <input name="limit" type="number" min="1" value="${policy.limit}" required />
      </div>

      <div class="field">
        <label>WindowInSeconds</label>
        <input name="windowInSeconds" type="number" min="1" value="${policy.windowInSeconds}" required />
      </div>
    </div>

    <div class="actions">
      <button class="btn primary" type="submit">Save changes</button>
      <button class="btn" type="button" id="btnClose">Cancel</button>
    </div>
  `;

  form.addEventListener("submit", async (e) => {
    e.preventDefault();
    await handleUpdatePolicy(policy.id, form, overlay);
  });

  form.querySelector("#btnClose").addEventListener("click", () => overlay.remove());

  modal.appendChild(form);
  overlay.appendChild(modal);
  document.body.appendChild(overlay);
}

async function handleUpdatePolicy(policyId, form, overlay) {
  const name = form.name.value.trim();
  const endpointPattern = form.endpointPattern.value.trim();
  const algorithm = form.algorithm.value;
  const limit = Number(form.limit.value);
  const windowInSeconds = Number(form.windowInSeconds.value);
  const burstLimitRaw = form.burstLimit.value.trim();
  const burstLimit = burstLimitRaw ? Number(burstLimitRaw) : null;

  if (!name) return setStatus("Name is required.", "warn");
  if (!endpointPattern) return setStatus("EndpointPattern is required.", "warn");
  if (!limit || limit <= 0) return setStatus("Limit must be positive.", "warn");
  if (!windowInSeconds || windowInSeconds <= 0) return setStatus("WindowInSeconds must be positive.", "warn");

  try {
    setStatus(`Updating policy ${policyId}...`, "muted");

    const updated = await api.put(`/admin/policies/${policyId}`, {
      name,
      endpointPattern,
      algorithm,
      limit,
      windowInSeconds,
      burstLimit
    });

    setStatus(`Updated policy ${updated.id}.`, "good");
    overlay.remove();
    showPage("policies");
  } catch (err) {
    setStatus(err.message, "bad");
  }
}

function escapeHtml(s) {
  return String(s)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

async function renderMetricsPage() {
  const root = $("#page-metrics");
  clearEl(root);

  const card = createEl("div", "card");
  card.appendChild(createEl("h2", "", "Metrics"));
  card.appendChild(createEl("div", "muted", "Basic observability. Data comes from usage_logs via /admin/metrics endpoints."));
  root.appendChild(card);

  const controls = createEl("div", "card");
  controls.appendChild(createEl("h2", "", "Controls"));

  const form = document.createElement("form");
  form.innerHTML = `
    <div class="form-row">
      <div class="field">
        <label>windowSeconds</label>
        <input name="windowSeconds" type="number" min="1" value="${state.lastWindowSeconds}" />
      </div>
      <div class="field">
        <label>&nbsp;</label>
        <button class="btn primary" type="submit">Load metrics</button>
      </div>
    </div>

    <div class="small muted">
      Tip: Sliding window traffic writes to usage_logs. Token bucket (in-memory) does not.
    </div>
  `;

  controls.appendChild(form);
  root.appendChild(controls);

  const summaryCard = createEl("div", "card");
  summaryCard.appendChild(createEl("h2", "", "Per-user summary"));
  const summaryHost = createEl("div");
  summaryCard.appendChild(summaryHost);
  root.appendChild(summaryCard);

  const endpointsCard = createEl("div", "card");
  endpointsCard.appendChild(createEl("h2", "", "Per-endpoint (selected user)"));
  endpointsCard.appendChild(createEl("div", "muted small", "Click a user row above to load endpoint breakdown."));
  const endpointsHost = createEl("div");
  endpointsCard.appendChild(endpointsHost);
  root.appendChild(endpointsCard);

  form.addEventListener("submit", async (e) => {
    e.preventDefault();
    const ws = Number(form.windowSeconds.value);
    if (!ws || ws <= 0) return setStatus("windowSeconds must be positive.", "warn");
    state.lastWindowSeconds = ws;
    await loadUserMetrics(summaryHost, endpointsHost, ws);
  });

  await loadUserMetrics(summaryHost, endpointsHost, state.lastWindowSeconds);
}

async function loadUserMetrics(summaryHost, endpointsHost, windowSeconds) {
  clearEl(endpointsHost);

  try {
    setStatus(`Loading metrics (windowSeconds=${windowSeconds})...`, "muted");
    const users = await api.get(`/admin/metrics/users?windowSeconds=${encodeURIComponent(windowSeconds)}`);

    renderTable(summaryHost, [
      { key: "userId", title: "UserId" },
      { key: "name", title: "Name" },
      { key: "email", title: "Email" },
      { key: "apiKeysCount", title: "Keys" },
      { key: "policiesCount", title: "Policies" },
      { key: "totalRequests", title: "Requests (window)" },
      { title: "LastRequestAtUtc", render: (row) => createEl("span", "", fmtUtc(row.lastRequestAtUtc)) },
      {
        title: "Actions",
        render: (row) => {
          const btn = createEl("button", "btn small", "Endpoints");
          btn.addEventListener("click", async () => {
            await loadEndpointMetrics(endpointsHost, row.userId, windowSeconds);
          });
          return btn;
        }
      }
    ], users);

    setStatus(`Loaded metrics for ${users.length} user(s).`, "good");
  } catch (err) {
    setStatus(err.message, "bad");
    clearEl(summaryHost);
    summaryHost.appendChild(createEl("div", "muted", "Failed to load metrics."));
  }
}

async function loadEndpointMetrics(host, userId, windowSeconds) {
  clearEl(host);

  try {
    setStatus(`Loading endpoint metrics for userId=${userId}...`, "muted");
    const rows = await api.get(`/admin/metrics/users/${userId}?windowSeconds=${encodeURIComponent(windowSeconds)}`);

    if (!rows.length) {
      host.appendChild(createEl("div", "muted", "No traffic in this time window."));
      setStatus(`No endpoint traffic for userId=${userId} in this window.`, "warn");
      return;
    }

    renderTable(host, [
      { key: "endpoint", title: "Endpoint" },
      { key: "requestCount", title: "RequestCount" },
      { title: "LastRequestAtUtc", render: (row) => createEl("span", "", fmtUtc(row.lastRequestAtUtc)) }
    ], rows);

    setStatus(`Loaded ${rows.length} endpoint row(s) for userId=${userId}.`, "good");
  } catch (err) {
    setStatus(err.message, "bad");
    host.appendChild(createEl("div", "muted", "Failed to load endpoint metrics."));
  }
}

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

function wireSidebarNavigation() {
  const navButtons = document.querySelectorAll(".nav-item");
  for (const btn of navButtons) {
    btn.addEventListener("click", () => {
      const page = btn.dataset.page;
      showPage(page);
    });
  }
}

async function startApp() {
  wireSidebarNavigation();
  await wireTopbarButtons();
  showPage("users");
}

startApp().catch((err) => {
  setStatus(`Startup error: ${err.message}`, "bad");
});
