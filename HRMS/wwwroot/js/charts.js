window.hrms = window.hrms || {};

hrms.renderEngagementChart = function (canvasId, labels, data) {
  if (typeof Chart === "undefined") {
    return;
  }

  const canvas = document.getElementById(canvasId);
  if (!canvas) {
    return;
  }

  const ctx = canvas.getContext("2d");
  new Chart(ctx, {
    type: "line",
    data: {
      labels: labels,
      datasets: [{ data: data, borderColor: "#3B5BDB", tension: 0.4 }]
    },
    options: {
      responsive: true,
      plugins: { legend: { display: false } }
    }
  });
};

hrms.renderAttendanceChart = function (canvasId, labels, data) {
  if (typeof Chart === "undefined") {
    return;
  }

  const canvas = document.getElementById(canvasId);
  if (!canvas) {
    return;
  }

  const ctx = canvas.getContext("2d");
  new Chart(ctx, {
    type: "line",
    data: {
      labels: labels,
      datasets: [{ data: data, borderColor: "#3B5BDB", tension: 0.4 }]
    },
    options: {
      responsive: true,
      plugins: { legend: { display: false } }
    }
  });
};
