(() => {
  "use strict";

  const root = document.getElementById("view-root");
  const title = document.getElementById("page-title");
  const lastRefresh = document.getElementById("last-refresh");
  const modal = document.getElementById("modal");
  const modalTitle = document.getElementById("modal-title");
  const modalBody = document.getElementById("modal-body");
  const modalOk = document.getElementById("modal-ok");
  const modalCancel = document.getElementById("modal-cancel");
  const modalClose = document.getElementById("modal-close");

  const base = (() => {
    const b = document.querySelector("base");
    if (b && b.href) {
      try { return new URL(b.href).pathname.replace(/\/$/, ""); } catch { /* fallthrough */ }
    }
    const path = window.location.pathname.replace(/\/$/, "");
    return path.endsWith("/index.html") ? path.slice(0, -11) : path;
  })();

  let currentView = "overview";
  let modalResolver = null;
  let chartMode = "day";
  let refreshTimer = null;
  let cachedOverview = null;
  let cachedHistory = [];

  const titles = {
    overview: "Dashboard",
    jobs: "Jobs",
    recurring: "Recurring Jobs",
    executing: "Currently Executing",
    http: "HTTP Jobs",
    history: "Retries & History",
    servers: "Servers"
  };

  // ── Navigation ──
  document.querySelectorAll(".nav-link").forEach((btn) => {
    btn.addEventListener("click", () => {
      document.querySelectorAll(".nav-link").forEach((b) => b.classList.remove("active"));
      btn.classList.add("active");
      currentView = btn.dataset.view;
      title.textContent = titles[currentView] || currentView;
      refresh();
    });
  });

  document.getElementById("btn-refresh").addEventListener("click", refresh);
  modalCancel.addEventListener("click", () => closeModal(false));
  modalClose.addEventListener("click", () => closeModal(false));
  modalOk.addEventListener("click", () => closeModal(true));
  modal.querySelector(".modal-backdrop").addEventListener("click", () => closeModal(false));

  // ── API ──
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

  function fmtTime(dt) {
    if (!dt) return "-";
    try { return new Date(dt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit", second: "2-digit" }); } catch { return String(dt); }
  }

  function stateBadge(state) {
    const s = (state || "").toLowerCase();
    let cls = "info";
    if (s === "normal") cls = "ok";
    else if (s === "paused") cls = "warn";
    else if (s === "error" || s === "blocked") cls = "danger";
    else if (s === "none") cls = "neutral";
    return `<span class="badge ${cls}">${esc(state || "None")}</span>`;
  }

  // ── Canvas chart (area chart) ──
  class AreaChart {
    constructor(canvas) {
      this.canvas = canvas;
      this.ctx = canvas.getContext("2d");
      this.labels = [];
      this.values = [];
      this._ro = new ResizeObserver(() => this.draw());
      this._ro.observe(canvas.parentElement);
    }

    setData(labels, values) {
      this.labels = labels;
      this.values = values;
      this.draw();
    }

    draw() {
      const canvas = this.canvas;
      const parent = canvas.parentElement;
      const dpr = window.devicePixelRatio || 1;
      const w = parent.clientWidth;
      const h = parent.clientHeight;
      canvas.width = w * dpr;
      canvas.height = h * dpr;
      canvas.style.width = w + "px";
      canvas.style.height = h + "px";

      const ctx = this.ctx;
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
      ctx.clearRect(0, 0, w, h);

      const pad = { top: 16, right: 16, bottom: 32, left: 44 };
      const cw = w - pad.left - pad.right;
      const ch = h - pad.top - pad.bottom;

      const maxVal = Math.max(1, ...this.values);
      const yMax = Math.ceil(maxVal * 1.15);

      // Grid
      ctx.strokeStyle = "#e8e8e8";
      ctx.lineWidth = 1;
      ctx.fillStyle = "#999";
      ctx.font = "11px Segoe UI, sans-serif";
      const ySteps = 5;
      for (let i = 0; i <= ySteps; i++) {
        const y = pad.top + (ch / ySteps) * i;
        const val = Math.round(yMax - (yMax / ySteps) * i);
        ctx.beginPath();
        ctx.moveTo(pad.left, y);
        ctx.lineTo(w - pad.right, y);
        ctx.stroke();
        ctx.textAlign = "right";
        ctx.fillText(String(val), pad.left - 6, y + 4);
      }

      if (this.values.length === 0) {
        ctx.textAlign = "center";
        ctx.fillStyle = "#aaa";
        ctx.font = "13px Segoe UI, sans-serif";
        ctx.fillText("No data yet — jobs will appear here after execution", w / 2, h / 2);
        return;
      }

      const step = cw / Math.max(1, this.values.length - 1);
      const points = this.values.map((v, i) => ({
        x: pad.left + i * step,
        y: pad.top + ch - (v / yMax) * ch
      }));

      // Area fill
      const grad = ctx.createLinearGradient(0, pad.top, 0, pad.top + ch);
      grad.addColorStop(0, "rgba(0, 166, 90, 0.55)");
      grad.addColorStop(1, "rgba(0, 166, 90, 0.05)");
      ctx.beginPath();
      ctx.moveTo(points[0].x, pad.top + ch);
      points.forEach((p) => ctx.lineTo(p.x, p.y));
      ctx.lineTo(points[points.length - 1].x, pad.top + ch);
      ctx.closePath();
      ctx.fillStyle = grad;
      ctx.fill();

      // Line
      ctx.beginPath();
      ctx.strokeStyle = "#00a65a";
      ctx.lineWidth = 2;
      points.forEach((p, i) => (i === 0 ? ctx.moveTo(p.x, p.y) : ctx.lineTo(p.x, p.y)));
      ctx.stroke();

      // X labels (sparse)
      ctx.fillStyle = "#999";
      ctx.textAlign = "center";
      const labelStep = Math.max(1, Math.floor(this.labels.length / 8));
      this.labels.forEach((lbl, i) => {
        if (i % labelStep === 0 || i === this.labels.length - 1) {
          ctx.fillText(lbl, pad.left + i * step, h - 8);
        }
      });
    }
  }

  function buildRealtimeChart(history) {
    const buckets = 24;
    const now = Date.now();
    const span = 2 * 60 * 1000; // 2 minutes, 5s buckets
    const bucketMs = span / buckets;
    const counts = new Array(buckets).fill(0);
    const labels = [];

    for (let i = 0; i < buckets; i++) {
      const t = now - span + i * bucketMs;
      labels.push(new Date(t).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit", second: "2-digit" }));
    }

    (history || []).forEach((h) => {
      const ts = new Date(h.firedAtUtc).getTime();
      if (ts < now - span) return;
      const idx = Math.min(buckets - 1, Math.max(0, Math.floor((ts - (now - span)) / bucketMs)));
      counts[idx]++;
    });

    return { labels, values: counts };
  }

  function buildHistoricalChart(history, mode) {
    const now = new Date();
    if (mode === "week") {
      const days = 7;
      const labels = [];
      const counts = new Array(days).fill(0);
      for (let i = days - 1; i >= 0; i--) {
        const d = new Date(now);
        d.setDate(d.getDate() - i);
        labels.push(d.toLocaleDateString([], { weekday: "short", month: "short", day: "numeric" }));
      }
      (history || []).forEach((h) => {
        const fd = new Date(h.firedAtUtc);
        const diff = Math.floor((now - fd) / 86400000);
        if (diff >= 0 && diff < days) counts[days - 1 - diff]++;
      });
      return { labels, values: counts };
    }

    // Day mode — hourly buckets
    const hours = 24;
    const labels = [];
    const counts = new Array(hours).fill(0);
    for (let i = 0; i < hours; i++) {
      labels.push(`${String(i).padStart(2, "0")}:00`);
    }
    (history || []).forEach((h) => {
      const fd = new Date(h.firedAtUtc);
      if (fd.toDateString() !== now.toDateString()) return;
      counts[fd.getHours()]++;
    });
    return { labels, values: counts };
  }

  // ── Nav badge counts ──
  async function updateNavCounts() {
    try {
      const [overview, jobs, recurring, executing, history] = await Promise.all([
        api("/api/overview"),
        api("/api/jobs"),
        api("/api/recurring"),
        api("/api/executing"),
        api("/api/history?take=200")
      ]);
      cachedOverview = overview;
      cachedHistory = history || [];

      setCount("nav-jobs", overview.jobCount);
      setCount("nav-recurring", recurring.length);
      setCount("nav-executing", overview.executingCount);
      setCount("nav-retries", overview.recentFailureCount);
      setCount("nav-http", (jobs || []).filter((j) => (j.jobType || "").includes("HttpInvokeJob")).length);
      setCount("nav-servers", 1);
    } catch { /* silent */ }
  }

  function setCount(id, n) {
    const el = document.getElementById(id);
    if (el) el.textContent = String(n ?? 0);
  }

  // ── Job actions ──
  function jobActions(job) {
    const g = encodeURIComponent(job.group);
    const n = encodeURIComponent(job.jobName);
    return `<div class="btn-group">
      <button class="btn btn-sm" data-act="pause" data-g="${g}" data-n="${n}">Pause</button>
      <button class="btn btn-sm" data-act="resume" data-g="${g}" data-n="${n}">Resume</button>
      <button class="btn btn-sm btn-success" data-act="trigger" data-g="${g}" data-n="${n}">Trigger</button>
      <button class="btn btn-sm" data-act="cron" data-g="${g}" data-n="${n}" data-cron="${esc(job.cronExpression || "")}">Cron</button>
      <button class="btn btn-sm" data-act="detail" data-g="${g}" data-n="${n}">Detail</button>
      <button class="btn btn-sm btn-danger" data-act="delete" data-g="${g}" data-n="${n}">Delete</button>
    </div>`;
  }

  function bindJobActions(container) {
    container.querySelectorAll("[data-act]").forEach((btn) => {
      btn.addEventListener("click", async () => {
        const act = btn.dataset.act;
        const g = decodeURIComponent(btn.dataset.g);
        const n = decodeURIComponent(btn.dataset.n);
        try {
          if (act === "delete") {
            const ok = await openConfirm(`Delete job "${n}"?`, "This action cannot be undone.");
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

  function jobsTable(jobs, emptyMsg) {
    if (!jobs || !jobs.length) {
      return `<div class="empty">${esc(emptyMsg || "No jobs found.")}</div>`;
    }
    return `<div class="panel"><div class="table-wrap"><table class="data-table">
      <thead><tr>
        <th>Job Name</th><th>Group</th><th>State</th><th>Cron</th><th>Next Fire</th><th>Running</th><th>Actions</th>
      </tr></thead>
      <tbody>${jobs.map((j) => `<tr>
        <td><strong>${esc(j.jobName)}</strong></td>
        <td>${esc(j.group)}</td>
        <td>${stateBadge(j.triggerState)}</td>
        <td><code>${esc(j.cronExpression || "-")}</code></td>
        <td>${esc(fmt(j.nextFireTime))}</td>
        <td>${j.isCurrentlyExecuting ? '<span class="badge ok">Running</span>' : '<span class="badge neutral">Idle</span>'}</td>
        <td>${jobActions(j)}</td>
      </tr>`).join("")}</tbody></table></div></div>`;
  }

  // ── Views ──
  async function renderOverview() {
    const o = cachedOverview || await api("/api/overview");
    const history = cachedHistory.length ? cachedHistory : await api("/api/history?take=500");

    root.innerHTML = `
      <div class="stat-row">
        <div class="stat-box green">
          <div class="stat-value">${o.jobCount}</div>
          <div class="stat-label">Jobs</div>
          <div class="stat-icon">&#9881;</div>
        </div>
        <div class="stat-box">
          <div class="stat-value">${o.triggerCount}</div>
          <div class="stat-label">Triggers</div>
          <div class="stat-icon">&#9201;</div>
        </div>
        <div class="stat-box yellow">
          <div class="stat-value">${o.executingCount}</div>
          <div class="stat-label">Executing</div>
          <div class="stat-icon">&#9654;</div>
        </div>
        <div class="stat-box">
          <div class="stat-value">${o.pausedTriggerCount}</div>
          <div class="stat-label">Paused</div>
        </div>
        <div class="stat-box red">
          <div class="stat-value">${o.recentFailureCount}</div>
          <div class="stat-label">Failures</div>
        </div>
        <div class="stat-box green">
          <div class="stat-value">${o.recentSuccessCount}</div>
          <div class="stat-label">Succeeded</div>
        </div>
      </div>

      <div class="panel">
        <div class="panel-header">
          <h3>Real-time Graph</h3>
          <span class="muted">Last 2 minutes · auto refresh</span>
        </div>
        <div class="panel-body">
          <div class="chart-wrap"><canvas id="chart-realtime"></canvas></div>
        </div>
      </div>

      <div class="panel">
        <div class="panel-header">
          <h3>History Graph</h3>
          <div class="chart-toggle">
            <button type="button" id="chart-day" class="${chartMode === "day" ? "active" : ""}">Day</button>
            <button type="button" id="chart-week" class="${chartMode === "week" ? "active" : ""}">Week</button>
          </div>
        </div>
        <div class="panel-body">
          <div class="chart-wrap"><canvas id="chart-history"></canvas></div>
        </div>
      </div>

      <div class="panel">
        <div class="panel-header">
          <h3>Scheduler</h3>
          <span class="badge ${o.isStarted ? "ok" : "warn"}">${o.isStarted ? "Running" : "Stopped"}</span>
        </div>
        <dl class="info-grid">
          <dt>Scheduler Name</dt><dd>${esc(o.schedulerName)}</dd>
          <dt>Instance Id</dt><dd><code>${esc(o.schedulerInstanceId)}</code></dd>
          <dt>Job Store</dt><dd>${esc(o.storeType)}</dd>
          <dt>Standby Mode</dt><dd>${o.inStandbyMode ? '<span class="badge warn">Yes</span>' : '<span class="badge ok">No</span>'}</dd>
          <dt>Server Time (UTC)</dt><dd>${esc(fmt(o.serverTimeUtc))}</dd>
        </dl>
      </div>`;

    const rt = buildRealtimeChart(history);
    const rtChart = new AreaChart(document.getElementById("chart-realtime"));
    rtChart.setData(rt.labels, rt.values);

    const hist = buildHistoricalChart(history, chartMode);
    const histChart = new AreaChart(document.getElementById("chart-history"));
    histChart.setData(hist.labels, hist.values);

    document.getElementById("chart-day").addEventListener("click", () => {
      chartMode = "day";
      refresh();
    });
    document.getElementById("chart-week").addEventListener("click", () => {
      chartMode = "week";
      refresh();
    });
  }

  async function renderJobs() {
    const jobs = await api("/api/jobs");
    root.innerHTML = jobsTable(jobs, "No jobs registered yet.");
    bindJobActions(root);
  }

  async function renderRecurring() {
    const jobs = await api("/api/recurring");
    root.innerHTML = jobsTable(jobs, "No recurring (cron) jobs found.");
    bindJobActions(root);
  }

  async function renderExecuting() {
    const jobs = await api("/api/executing");
    root.innerHTML = jobsTable(jobs, "No jobs are currently executing.");
    bindJobActions(root);
  }

  async function renderHttp() {
    root.innerHTML = `
      <div class="panel">
        <div class="panel-header"><h3>Create / Update HTTP Job</h3></div>
        <div class="panel-body">
          <div class="form-grid">
            <label>Job Name<input id="http-name" placeholder="MyHttpJob" /></label>
            <label>Job Group<input id="http-group" value="DEFAULT" /></label>
            <label class="full">URL<input id="http-url" placeholder="http://localhost:5102/demo/ping" /></label>
            <label>Method
              <select id="http-method">
                <option>GET</option><option>POST</option><option>PUT</option><option>DELETE</option><option>PATCH</option>
              </select>
            </label>
            <label>Cron<input id="http-cron" value="0/30 * * * * ?" /></label>
            <label class="full">Headers (JSON)<textarea id="http-headers" placeholder='{"Authorization":"Bearer ..."}'></textarea></label>
            <label class="full">Body<textarea id="http-body" placeholder='{"id":1,"title":"hello"}'></textarea></label>
            <label class="full">Description<input id="http-desc" placeholder="Optional description" /></label>
          </div>
          <div style="margin-top:16px;">
            <button type="button" id="http-save" class="btn btn-primary">Save HTTP Job</button>
            <span id="http-msg" class="muted" style="margin-left:12px;"></span>
          </div>
        </div>
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
        msg.textContent = result.message || "Saved successfully.";
        msg.style.color = "var(--hf-green)";
        await loadHttpList();
        await updateNavCounts();
      } catch (err) {
        msg.textContent = err.message || String(err);
        msg.style.color = "var(--hf-red)";
      }
    });

    await loadHttpList();
  }

  async function loadHttpList() {
    const jobs = await api("/api/jobs");
    const httpJobs = (jobs || []).filter((j) => (j.jobType || "").includes("HttpInvokeJob"));
    const el = document.getElementById("http-list");
    if (!el) return;
    el.innerHTML = `<div class="panel"><div class="panel-header"><h3>HTTP Jobs (${httpJobs.length})</h3></div></div>`
      + jobsTable(httpJobs, "No HTTP jobs yet. Create one above.");
    bindJobActions(el);
  }

  async function renderHistory() {
    const rows = await api("/api/history?take=200");
    const failures = (rows || []).filter((r) => !r.success);

    if (!rows || !rows.length) {
      root.innerHTML = `<div class="empty">No execution history yet.<br><small>History is stored in-memory on this node.</small></div>`;
      return;
    }

    root.innerHTML = `
      <div class="stat-row" style="margin-bottom:16px;">
        <div class="stat-box green"><div class="stat-value">${rows.filter(r => r.success).length}</div><div class="stat-label">Success</div></div>
        <div class="stat-box red"><div class="stat-value">${failures.length}</div><div class="stat-label">Failed</div></div>
        <div class="stat-box"><div class="stat-value">${rows.length}</div><div class="stat-label">Total (recent)</div></div>
      </div>
      <div class="panel"><div class="table-wrap"><table class="data-table">
        <thead><tr>
          <th>Fired At</th><th>Job</th><th>Group</th><th>Duration</th><th>Result</th><th>Error</th>
        </tr></thead>
        <tbody>${rows.map((r) => `<tr class="${r.success ? "" : "row-fail"}">
          <td>${esc(fmt(r.firedAtUtc))}</td>
          <td><strong>${esc(r.jobName)}</strong></td>
          <td>${esc(r.jobGroup)}</td>
          <td>${r.durationMs != null ? Math.round(r.durationMs) + " ms" : "-"}</td>
          <td>${r.success ? '<span class="badge ok">Success</span>' : '<span class="badge danger">Failed</span>'}</td>
          <td>${esc(r.exceptionMessage || "")}</td>
        </tr>`).join("")}</tbody>
      </table></div></div>
      <p class="note">History is process-local (in-memory ring buffer). It resets when the application restarts.</p>`;
  }

  async function renderServers() {
    const o = cachedOverview || await api("/api/overview");
    root.innerHTML = `
      <div class="panel">
        <div class="panel-header">
          <h3>Active Server</h3>
          <span class="badge ok">Online</span>
        </div>
        <dl class="info-grid">
          <dt>Scheduler Name</dt><dd>${esc(o.schedulerName)}</dd>
          <dt>Instance Id</dt><dd><code>${esc(o.schedulerInstanceId)}</code></dd>
          <dt>Job Store</dt><dd>${esc(o.storeType)}</dd>
          <dt>Started</dt><dd>${o.isStarted ? '<span class="badge ok">Yes</span>' : '<span class="badge warn">No</span>'}</dd>
          <dt>Standby Mode</dt><dd>${o.inStandbyMode ? '<span class="badge warn">Yes</span>' : '<span class="badge ok">No</span>'}</dd>
          <dt>Shutdown</dt><dd>${o.isShutdown ? '<span class="badge danger">Yes</span>' : '<span class="badge ok">No</span>'}</dd>
          <dt>Server Time (UTC)</dt><dd>${esc(fmt(o.serverTimeUtc))}</dd>
        </dl>
      </div>
      <p class="note">In clustered ADO job-store deployments, additional server nodes appear in Quartz scheduler state tables.</p>`;
  }

  // ── Modal ──
  function openConfirm(titleText, bodyHtml) {
    return new Promise((resolve) => {
      modalResolver = resolve;
      modalTitle.textContent = titleText;
      modalBody.innerHTML = `<p>${esc(bodyHtml)}</p>`;
      modalOk.style.display = "";
      modalCancel.textContent = "Cancel";
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
      modalBody.innerHTML = `<label class="full" style="display:flex;flex-direction:column;gap:6px;">
        <span style="font-weight:600;color:var(--hf-muted);">Cron expression</span>
        <input id="prompt-input" value="${esc(initial)}" /></label>`;
      modalOk.style.display = "";
      modalCancel.textContent = "Cancel";
      modal.classList.remove("hidden");
      setTimeout(() => document.getElementById("prompt-input")?.focus(), 50);
    });
  }

  function openDetail(job) {
    return new Promise((resolve) => {
      modalResolver = () => resolve(true);
      modalTitle.textContent = job.jobName;
      modalOk.style.display = "none";
      modalCancel.textContent = "Close";
      const data = job.jobData
        ? Object.entries(job.jobData).map(([k, v]) => `<dt>${esc(k)}</dt><dd>${esc(v)}</dd>`).join("")
        : "";
      modalBody.innerHTML = `<dl class="info-grid">
        <dt>Group</dt><dd>${esc(job.group)}</dd>
        <dt>Type</dt><dd><code>${esc(job.jobType || "-")}</code></dd>
        <dt>State</dt><dd>${stateBadge(job.triggerState)}</dd>
        <dt>Cron</dt><dd><code>${esc(job.cronExpression || "-")}</code></dd>
        <dt>Next Fire</dt><dd>${esc(fmt(job.nextFireTime))}</dd>
        <dt>Previous Fire</dt><dd>${esc(fmt(job.previousFireTime))}</dd>
        <dt>Description</dt><dd>${esc(job.description || "-")}</dd>
        ${data}
      </dl>`;
      modal.classList.remove("hidden");
    });
  }

  function closeModal(ok) {
    modal.classList.add("hidden");
    modalOk.style.display = "";
    const r = modalResolver;
    modalResolver = null;
    if (r) r(ok);
  }

  // ── Refresh loop ──
  async function refresh() {
    clearInterval(refreshTimer);
    try {
      root.innerHTML = `<div class="loading">Loading</div>`;
      await updateNavCounts();

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

    if (currentView === "overview" || currentView === "executing") {
      refreshTimer = setInterval(async () => {
        await updateNavCounts();
        if (currentView === "overview") await renderOverview();
        else if (currentView === "executing") await renderExecuting();
        lastRefresh.textContent = `Updated ${new Date().toLocaleTimeString()}`;
      }, 5000);
    }
  }

  refresh();
})();
