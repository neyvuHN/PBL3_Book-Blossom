/**
 * BOOKBLOSSOM MESSAGES PAGE - MVC MODULE INITIATOR
 */
console.log("BookBlossom: Modular MVC messages files active.");

$(document).ready(function () {
    const model = new window.MessagesModel();
    const view = new window.MessagesView();
    const controller = new window.MessagesController(model, view);
    controller.init();
});
