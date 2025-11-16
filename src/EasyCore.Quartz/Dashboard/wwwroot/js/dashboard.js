(() => {
  const root = document.getElementById("view-root");
  const title = document.getElementById("page-title");
  const lastRefresh = document.getElementById("last-refresh");
  const modal = document.getElementById("modal");
  const modalTitle = document.getElementById("modal-title");
  const modalBody = document.getElementById("modal-body");
  const modalOk = document.getElementById("modal-ok");
  const modalCancel = document.getElementById("modal-cancel");

  const base = (() => {
    const path = window.location.pathname.replace(/\/$/, "");
    return path.endsWith("/index.html") ? path.slice(0, -11) : path;
  })();

  let currentView = "overview";
  let modalResolver = null;

  const titles = {
    overview: "Overview",
    jobs: "Jobs",
    recurring: "Recurring Jobs",
    executing: "Currently Executing",
    http: "HTTP Jobs",
    history: "Execution History",
    servers: "Servers"
  };

  document.querySelectorAll(".nav-item").forEach((btn) => {
    btn.addEventListener("click", () => {
      document.querySelectorAll(".nav-item").forEach((b) => b.classList.remove("active"));
      btn.classList.add("active");
      currentView = btn.dataset.view;
      title.textContent = titles[currentView] || currentView;
      refresh();
    });
  });

  document.getElementById("btn-refresh").addEventListener("click", refresh);
  modalCancel.addEventListener("click", () => closeModal(false));
  modalOk.addEventListener("click", () => closeModal(true));

  async function api(path, options) {
    const res = await fetch(`${base}${path}`, {
      headers: { "Content-Type": "application/json", ...(options && options.headers) },
      ...options
    });
    const text = await res.text();
    let data = null;
    try { data = text ? JSON.parse(text) : null; } catch { data = { error: text }; }
    if (!res.ok) {
      throw new Error((data && (data.error || data.message)) || res.statusText || "Request failed");
    }
    return data;
  }

  function esc(v) {
    return String(v ?? "").replace(/[&<>"']/g, (c) => ({
      "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;"
    }[c]));
  }

  function fmt(dt) {
    if (!dt) return "-";
    try { return new Date(dt).toLocaleString(); } catch { return String(dt); }
  }

  function stateBadge(state) {
    const s = (state || "").toLowerCase();
    let cls = "info";
    if (s === "normal") cls = "ok";
    else if (s === "paused") cls = "warn";
    else if (s === "error" || s === "blocked") cls = "danger";
    return `<span class="badge ${cls}">${esc(state || "None")}</span>`;
  }

  function jobActions(job) {
    const g = encodeURIComponent(job.group);
    const n = encodeURIComponent(job.jobName);
    return `
      <button class="btn btn-sm" data-act="pause" data-g="${g}" data-n="${n}">Pause</button>
      <button class="btn btn-sm" data-act="resume" data-g="${g}" data-n="${n}">Resume</button>
      <button class="btn btn-sm" data-act="trigger" data-g="${g}" data-n="${n}">Trigger</button>
      <button class="btn btn-sm" data-act="cron" data-g="${g}" data-n="${n}" data-cron="${esc(job.cronExpression || "")}">Edit Cron</button>
      <button class="btn btn-sm" data-act="detail" data-g="${g}" data-n="${n}">Detail</button>
      <button class="btn btn-sm btn-danger" data-act="delete" data-g="${g}" data-n="${n}">Delete</button>`;
  }

  function bindJobActions(container) {
    container.querySelectorAll("[data-act]").forEach((btn) => {
      btn.addEventListener("click", async () => {
        const act = btn.dataset.act;
        const g = decodeURIComponent(btn.dataset.g);
        const n = decodeURIComponent(btn.dataset.n);
        try {
          if (act === "delete") {
            const ok = await openConfirm(`Delete job ${n}?`, "This cannot be undone.");
            if (!ok) return;
            await api(`/api/jobs/${encodeURIComponent(g)}/${encodeURIComponent(n)}/delete`, { method: "POST" });
          } else if (act === "cron") {
            const cron = await openPrompt("Update cron expression", btn.dataset.cron || "0/30 * * * * ?");
            if (cron == null) return;
            await api(`/api/jobs/${encodeURIComponent(g)}/${encodeURIComponent(n)}/cron`, {
              method: "POST",
              body: JSON.stringify({ cron })
            });
          } else if (act === "detail") {
            const job = await api(`/api/jobs/${encodeURIComponent(g)}/${encodeURIComponent(n)}`);
            await openDetail(job);
            return;
          } else {
            await api(`/api/jobs/${encodeURIComponent(g)}/${encodeURIComponent(n)}/${act}`, { method: "POST" });
          }
          await refresh();
        } catch (err) {
          alert(err.message || String(err));
        }
      });
    });
  }

  function jobsTable(jobs) {
    if (!jobs || !jobs.length) return `<div class="empty">No jobs found.</div>`;
    return `<div class="panel"><table>
      <thead><tr>
        <th>Job</th><th>Group</th><th>State</th><th>Cron</th><th>Next Fire</th><th>Executing</th><th>Actions</th>
      </tr></thead>
      <tbody>${jobs.map((j) => `<tr>
        <td>${esc(j.jobName)}</td>
        <td>${esc(j.group)}</td>
        <td>${stateBadge(j.triggerState)}</td>
        <td><code>${esc(j.cronExpression || "-")}</code></td>
        <td>${esc(fmt(j.nextFireTime))}</td>
        <td>${j.isCurrentlyExecuting ? '<span class="badge ok">Yes</span>' : '<span class="badge">No</span>'}</td>
        <td>${jobActions(j)}</td>
      </tr>`).join("")}</tbody></table></div>`;
  }

  async function renderOverview() {
    const o = await api("/api/overview");
    root.innerHTML = `
      <div class="cards">
        <div class="card"><div class="label">Jobs</div><div class="value">${o.jobCount}</div></div>
        <div class="card"><div class="label">Triggers</div><div class="value">${o.triggerCount}</div></div>
        <div class="card"><div class="label">Executing</div><div class="value">${o.executingCount}</div></div>
        <div class="card"><div class="label">Paused</div><div class="value">${o.pausedTriggerCount}</div></div>
        <div class="card"><div class="label">Errors</div><div class="value">${o.errorTriggerCount}</div></div>
        <div class="card"><div class="label">Recent Failures</div><div class="value">${o.recentFailureCount}</div></div>
      </div>
      <div class="panel">
        <div class="panel-header"><strong>Scheduler</strong><span class="badge ${o.isStarted ? "ok" : "warn"}">${o.isStarted ? "Started" : "Stopped"}</span></div>
        <table>
          <tr><th>Name</th><td>${esc(o.schedulerName)}</td></tr>
          <tr><th>Instance Id</th><td>${esc(o.schedulerInstanceId)}</td></tr>
          <tr><th>Store</th><td>${esc(o.storeType)}</td></tr>
          <tr><th>Standby</th><td>${o.inStandbyMode}</td></tr>
          <tr><th>Server UTC</th><td>${esc(fmt(o.serverTimeUtc))}</td></tr>
          <tr><th>Recent Success</th><td>${o.recentSuccessCount}</td></tr>
        </table>
      </div>`;
  }

  async function renderJobs() {
    const jobs = await api("/api/jobs");
    root.innerHTML = jobsTable(jobs);
    bindJobActions(root);
  }

  async function renderRecurring() {
    const jobs = await api("/api/recurring");
    root.innerHTML = jobsTable(jobs);
    bindJobActions(root);
  }

  async function renderExecuting() {
    const jobs = await api("/api/executing");
    root.innerHTML = jobsTable(jobs);
    bindJobActions(root);
  }

  async function renderHttp() {
    root.innerHTML = `
      <div class="panel" style="padding:16px;margin-bottom:16px;">
        <h3 style="margin-top:0;">Create / Update HTTP Job</h3>
        <div class="form-grid">
          <label>Job Name<input id="http-name" placeholder="MyHttpJob" /></label>
          <label>Job Group<input id="http-group" value="DEFAULT" /></label>
          <label class="full">URL<input id="http-url" placeholder="https://example.com/api/ping" /></label>
          <label>Method
            <select id="http-method">
              <option>GET</option><option>POST</option><option>PUT</option><option>DELETE</option><option>PATCH</option>
            </select>
          </label>
          <label>Cron<input id="http-cron" value="0/30 * * * * ?" /></label>
          <label class="full">Headers (JSON object)<textarea id="http-headers" placeholder='{"Authorization":"Bearer ..."}'></textarea></label>
          <label class="full">Body<textarea id="http-body" placeholder='{"ok":true}'></textarea></label>
          <label class="full">Description<input id="http-desc" placeholder="Optional description" /></label>
        </div>
        <div style="margin-top:12px;"><button id="http-save" class="btn btn-primary">Save HTTP Job</button></div>
        <div id="http-msg" class="muted" style="margin-top:8px;"></div>
      </div>
      <div id="http-list"></div>`;

    document.getElementById("http-save").addEventListener("click", async () => {
      const msg = document.getElementById("http-msg");
      try {
        let headers = null;
        const headersRaw = document.getElementById("http-headers").value.trim();
        if (headersRaw) headers = JSON.parse(headersRaw);
        const payload = {
          jobName: document.getElementById("http-name").value.trim(),
          jobGroup: document.getElementById("http-group").value.trim() || "DEFAULT",
          url: document.getElementById("http-url").value.trim(),
          method: document.getElementById("http-method").value,
          cron: document.getElementById("http-cron").value.trim(),
          body: document.getElementById("http-body").value,
          headers,
          description: document.getElementById("http-desc").value.trim()
        };
        const result = await api("/api/http-jobs", { method: "POST", body: JSON.stringify(payload) });
        msg.textContent = result.message || "Saved.";
        await loadHttpList();
      } catch (err) {
        msg.textContent = err.message || String(err);
      }
    });

    await loadHttpList();
  }

  async function loadHttpList() {
    const jobs = await api("/api/jobs");
    const httpJobs = (jobs || []).filter((j) => (j.jobType || "").includes("HttpInvokeJob"));
    const el = document.getElementById("http-list");
    if (!el) return;
    el.innerHTML = jobsTable(httpJobs);
    bindJobActions(el);
  }

  async function renderHistory() {
    const rows = await api("/api/history?take=200");
    if (!rows || !rows.length) {
      root.innerHTML = `<div class="empty">No execution history yet. History is process-local (in-memory).</div>`;
      return;
    }
    root.innerHTML = `<div class="panel"><table>
      <thead><tr>
        <th>Fired At (UTC)</th><th>Job</th><th>Group</th><th>Duration</th><th>Result</th><th>Error</th>
      </tr></thead>
      <tbody>${rows.map((r) => `<tr>
        <td>${esc(fmt(r.firedAtUtc))}</td>
        <td>${esc(r.jobName)}</td>
        <td>${esc(r.jobGroup)}</td>
        <td>${r.durationMs != null ? Math.round(r.durationMs) + " ms" : "-"}</td>
        <td>${r.success ? '<span class="badge ok">Success</span>' : '<span class="badge danger">Failed</span>'}</td>
        <td>${esc(r.exceptionMessage || "")}</td>
      </tr>`).join("")}</tbody></table></div>
      <p class="muted" style="margin-top:10px;">Note: history is stored in-memory on this node only.</p>`;
  }

  async function renderServers() {
    const o = await api("/api/overview");
    root.innerHTML = `<div class="panel"><div class="panel-header"><strong>This Node</strong></div>
      <table>
        <tr><th>Scheduler Name</th><td>${esc(o.schedulerName)}</td></tr>
        <tr><th>Instance Id</th><td>${esc(o.schedulerInstanceId)}</td></tr>
        <tr><th>Job Store</th><td>${esc(o.storeType)}</td></tr>
        <tr><th>Started</th><td>${o.isStarted}</td></tr>
        <tr><th>Standby Mode</th><td>${o.inStandbyMode}</td></tr>
        <tr><th>Shutdown</th><td>${o.isShutdown}</td></tr>
        <tr><th>Server Time (UTC)</th><td>${esc(fmt(o.serverTimeUtc))}</td></tr>
      </table></div>
      <p class="muted" style="margin-top:10px;">Cluster peer listing depends on the ADO job store and Quartz scheduler state tables.</p>`;
  }

  function openConfirm(titleText, bodyHtml) {
    return new Promise((resolve) => {
      modalResolver = resolve;
      modalTitle.textContent = titleText;
      modalBody.innerHTML = `<p>${esc(bodyHtml)}</p>`;
      modal.classList.remove("hidden");
    });
  }

  function openPrompt(titleText, initial) {
    return new Promise((resolve) => {
      modalResolver = (ok) => {
        if (!ok) return resolve(null);
        resolve(document.getElementById("prompt-input").value);
      };
      modalTitle.textContent = titleText;
      modalBody.innerHTML = `<label class="full" style="display:flex;flex-direction:column;gap:6px;color:var(--muted);font-size:12px;">
        Cron expression<input id="prompt-input" value="${esc(initial)}" /></label>`;
      modal.classList.remove("hidden");
    });
  }

  function openDetail(job) {
    return new Promise((resolve) => {
      modalResolver = () => resolve(true);
      modalTitle.textContent = `${job.jobName}`;
      const data = job.jobData
        ? Object.entries(job.jobData).map(([k, v]) => `<tr><th>${esc(k)}</th><td>${esc(v)}</td></tr>`).join("")
        : "";
      modalBody.innerHTML = `<table>
        <tr><th>Group</th><td>${esc(job.group)}</td></tr>
        <tr><th>Type</th><td>${esc(job.jobType || "-")}</td></tr>
        <tr><th>State</th><td>${stateBadge(job.triggerState)}</td></tr>
        <tr><th>Cron</th><td>${esc(job.cronExpression || "-")}</td></tr>
        <tr><th>Next</th><td>${esc(fmt(job.nextFireTime))}</td></tr>
        <tr><th>Previous</th><td>${esc(fmt(job.previousFireTime))}</td></tr>
        <tr><th>Description</th><td>${esc(job.description || "-")}</td></tr>
        ${data}
      </table>`;
      modal.classList.remove("hidden");
    });
  }

  function closeModal(ok) {
    modal.classList.add("hidden");
    const r = modalResolver;
    modalResolver = null;
    if (r) r(ok);
  }

  async function refresh() {
    try {
      root.innerHTML = `<div class="empty">Loading...</div>`;
      if (currentView === "overview") await renderOverview();
      else if (currentView === "jobs") await renderJobs();
      else if (currentView === "recurring") await renderRecurring();
      else if (currentView === "executing") await renderExecuting();
      else if (currentView === "http") await renderHttp();
      else if (currentView === "history") await renderHistory();
      else if (currentView === "servers") await renderServers();
      lastRefresh.textContent = `Updated ${new Date().toLocaleTimeString()}`;
    } catch (err) {
      root.innerHTML = `<div class="error">${esc(err.message || err)}</div>`;
    }
  }

  refresh();
  setInterval(() => {
    if (currentView === "overview" || currentView === "executing" || currentView === "history") {
      refresh();
    }
  }, 15000);
})();
