document.addEventListener("DOMContentLoaded", function () {
    document.getElementById("toggleSidebar").addEventListener("click", function () {
        document.getElementById("sidebar").classList.toggle("collapsed");
    });
});