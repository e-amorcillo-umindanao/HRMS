window.hrms = window.hrms || {};

hrms.initDropdown = function (triggerId, menuId) {
  document.addEventListener("click", function (e) {
    const trigger = document.getElementById(triggerId);
    const menu = document.getElementById(menuId);
    if (!trigger || !menu) {
      return;
    }

    if (!trigger.contains(e.target) && !menu.contains(e.target)) {
      menu.classList.add("hidden");
    }
  });
};
