window.hrms = window.hrms || {};

hrms.initTheme = function () {
  const saved = localStorage.getItem("hrms-theme");
  if (saved) {
    document.documentElement.setAttribute("data-theme", saved);
  }

  return document.documentElement.getAttribute("data-theme") ?? "lemonade";
};

hrms.getTheme = function () {
  return document.documentElement.getAttribute("data-theme") ?? "lemonade";
};

hrms.setTheme = function (theme) {
  document.documentElement.setAttribute("data-theme", theme);
  localStorage.setItem("hrms-theme", theme);
};

hrms.renderEngagementChart = function (canvasId, labels, data) {
  const canvas = document.getElementById(canvasId);
  if (!canvas || typeof Chart === "undefined") return;

  const existing = Chart.getChart(canvas);
  if (existing) existing.destroy();

  new Chart(canvas, {
    type: "line",
    data: {
      labels,
      datasets: [{
        data,
        borderColor: "#1c4f82",
        backgroundColor: "rgba(28,79,130,0.08)",
        tension: 0.4,
        fill: true,
        pointRadius: 3,
      }]
    },
    options: {
      responsive: true,
      plugins: { legend: { display: false } },
      scales: {
        y: { beginAtZero: true, max: 100, grid: { color: "rgba(0,0,0,0.05)" } },
        x: { grid: { display: false } }
      }
    }
  });
};

hrms.renderAttendanceChart = function (canvasId, labels, data) {
  const canvas = document.getElementById(canvasId);
  if (!canvas || typeof Chart === "undefined") return;

  const existing = Chart.getChart(canvas);
  if (existing) existing.destroy();

  new Chart(canvas, {
    type: "bar",
    data: {
      labels,
      datasets: [{
        data,
        backgroundColor: "rgba(103,203,160,0.7)",
        borderRadius: 3,
      }]
    },
    options: {
      responsive: true,
      plugins: { legend: { display: false } },
      scales: {
        y: { beginAtZero: true, grid: { color: "rgba(0,0,0,0.05)" } },
        x: { grid: { display: false } }
      }
    }
  });
};

hrms.showToast = function (message, type = "success") {
  let container = document.getElementById("toast-container");
  if (!container) {
    container = document.createElement("div");
    container.id = "toast-container";
    container.className = "toast toast-bottom toast-end z-[9999]";
    document.body.appendChild(container);
  }

  const alert = document.createElement("div");
  alert.className = `alert alert-${type} text-sm py-2 px-4 shadow-lg`;
  alert.innerHTML = `<span>${message}</span>`;
  container.appendChild(alert);
  setTimeout(() => alert.remove(), 3000);
};

hrms.destroyChart = function (canvasId) {
  const canvas = document.getElementById(canvasId);
  if (!canvas || typeof Chart === "undefined") return;
  const existing = Chart.getChart(canvas);
  if (existing) existing.destroy();
};
