/**
 * BOOKBLOSSOM ADMIN MESSAGES - MVC MODULE INITIATOR
 */
console.log("BookBlossom Admin: Modular MVC messages files active.");

$(document).ready(function () {
    const model = new window.AdminMessagesModel();
    const view = new window.AdminMessagesView();
    const controller = new window.AdminMessagesController(model, view);
    controller.init();
});
