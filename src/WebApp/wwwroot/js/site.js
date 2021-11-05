// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$(function () {
    var pageIdAttr = "data-menuPageId";
    var currentPage = $("#currentPageId").attr("value");

    var menu = $(".navbar-nav");

    $("li[" + pageIdAttr + "]").removeClass("active");
    $("li[" + pageIdAttr + "=\"" + currentPage + "\"]", menu).addClass("active");
});