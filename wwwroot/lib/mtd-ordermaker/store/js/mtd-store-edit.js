/*
    MTD OrderMaker - http://ordermaker.org
    Copyright (c) 2019 Oleg Bruev <job4bruev@gmail.com>. All rights reserved.

    This file is part of MTD OrderMaker.
    MTD OrderMaker is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see  https://www.gnu.org/licenses/.
*/

function pad(num, size) {
    var s = num + "";
    while (s.length < size) s = "0" + s;
    return s;
}

const ListenerCreate = (e) => {

    const spn = document.getElementById("store-parent-input");
    if (!spn) {
        const storeCreateButton = document.getElementById("store-create-button");
        if (storeCreateButton) {
            storeCreateButton.addEventListener('click', () => {
                document.getElementById('store-create-clicker').click();
            });
        }

        return;
    }

    const number = document.getElementById("store-parent-number");
    const parent = document.getElementById("store-parent-id");
    const form = document.getElementById("store-parent-form");
    const action = form.getAttribute('action');
    const iconCheck = document.getElementById("parent-result-check");
    const iconWarning = document.getElementById("parent-result-warning");
    const progress = document.getElementById("store-parent-progress");

    document.getElementById("store-create-button").addEventListener('click', () => {

        const xmlHttp = new XMLHttpRequest();
        const formData = CreateFormData(form);
        xmlHttp.open("post", action, true);
        xmlHttp.onreadystatechange = function (e) {
            if (xmlHttp.readyState == 4 && xmlHttp.status == 200) {
                setTimeout(() => {
                    if (this.responseText) {
                        iconCheck.style.display = "";
                        document.getElementById("store-base-field").classList.remove("mdc-text-field--invalid");
                        parent.value = this.responseText;
                        spn.value = pad(spn.value, 9);
                        document.getElementById('store-create-clicker').click();
                    } else {
                        iconWarning.style.display = "";
                        MainShowSnackBar("Неправильный номер документа!");
                    }
                    progress.classList.add("mdc-linear-progress--closed");
                }, 500);
            }
        }
        xmlHttp.send(formData);
    });
}

const ListenerForParent = () => {
    const spn = document.getElementById("store-parent-input");

    if (!spn) return;

    const number = document.getElementById("store-parent-number");
    const parent = document.getElementById("store-parent-id");
    const form = document.getElementById("store-parent-form");
    const action = form.getAttribute('action');
    const iconCheck = document.getElementById("parent-result-check");
    const iconWarning = document.getElementById("parent-result-warning");
    const progress = document.getElementById("store-parent-progress");

    spn.addEventListener('change', (e) => {
        progress.classList.remove("mdc-linear-progress--closed");
        number.value = spn.value;
        iconCheck.style.display = "none";
        iconWarning.style.display = "none";

        const xmlHttp = new XMLHttpRequest();
        const formData = CreateFormData(form);
        xmlHttp.open("post", action, true);
        xmlHttp.onreadystatechange = function (e) {
            if (xmlHttp.readyState == 4 && xmlHttp.status == 200) {
                setTimeout(() => {
                    if (this.responseText) {
                        iconCheck.style.display = "";
                        document.getElementById("store-base-field").classList.remove("mdc-text-field--invalid");
                        parent.value = this.responseText;
                        spn.value = pad(spn.value, 9);
                    } else {
                        iconWarning.style.display = "";
                        MainShowSnackBar("Неправильный номер документа!");
                    }
                    progress.classList.add("mdc-linear-progress--closed");
                }, 500);
            }
        }
        xmlHttp.send(formData);
    });
}


//Start

ListenerCreate();
ListenerForParent();

const tagName = "mtdSelector";
const items = document.querySelectorAll(`div[${tagName}]`);
items.forEach((item) => {

    const id = item.attributes.getNamedItem(tagName).nodeValue;
    const input = document.getElementById(id);
    const href = document.getElementById(`${id}-href`);
    const select = document.getElementById(`${id}-select`);
    const strike = document.getElementById(`${id}-strike`);

    const actionDelete = document.getElementById(`${id}-action-delete`);
    const actionUndo = document.getElementById(`${id}-action-undo`);
    const fixDelete = document.getElementById(`${id}-delete`);

    const isFile = href.firstElementChild.textContent;
    if (isFile) actionDelete.hidden = false;

    item.addEventListener("click", () => {
        input.click();
    });

    input.addEventListener("change", (event) => {
        select.innerHTML = event.target.files[0].name;
        href.hidden = true; select.hidden = false; strike.hidden = true;
        actionDelete.hidden = false;
        actionUndo.hidden = true;
        fixDelete.checked = false;
    });

    actionDelete.addEventListener("click", () => {

        if (input.value) {
            href.hidden = false; select.hidden = true; strike.hidden = true;
            input.value = null;
            if (!isFile) {
                actionDelete.hidden = true;
            }
        } else {
            href.hidden = true; select.hidden = true; strike.hidden = false;
            actionDelete.hidden = true;
            actionUndo.hidden = false;
            fixDelete.checked = true;
        }
    });

    actionUndo.addEventListener("click", () => {
        href.hidden = false; select.hidden = true; strike.hidden = true;
        actionDelete.hidden = false;
        actionUndo.hidden = true;
        fixDelete.checked = false;
    });

});

document.querySelectorAll('select[datalink]').forEach((datalink) => {
    const id = datalink.attributes.getNamedItem("datalink").nodeValue;
    const input = document.getElementById(`${id}-datalink`);

    const dlv = datalink.options[datalink.selectedIndex];
    if (dlv) {
        input.value = dlv.textContent;
        datalink.addEventListener('change', (e) => {
            document.getElementById(`${id}-datalink`).value = e.target.options[e.target.selectedIndex].textContent;
        });
    }

});

const dialog = document.getElementById('dialog-info');
if (dialog) {
    const dialogInfo = new mdc.dialog.MDCDialog(dialog);
    const dialogInfoContent = document.getElementById('dialog-info-content');
    const dialogInfoTitle = document.getElementById('dialog-info-title');
    document.querySelectorAll('[mtd-info]').forEach((item) => {
        item.addEventListener('click', (e) => {
            const note = item.getAttribute('mtd-info');
            dialogInfoTitle.innerHTML = e.target.textContent;
            dialogInfoContent.innerHTML = note;
            dialogInfo.open();

        });
    });
}

const textFields = document.querySelectorAll(".mdc-text-field");

textFields.forEach((textField) => {
    new MTDTextField(textField.id)
});

const selectFields = document.querySelectorAll(".mdc-select");
selectFields.forEach((selectField) => {
    new MTDSelectList(selectField.id)
});
    //document.addEventListener("keydown", (e) => {
    //    if (e.keyCode == 13) {
    //        event.preventDefault();
    //        return false;
    //    }
    //});

