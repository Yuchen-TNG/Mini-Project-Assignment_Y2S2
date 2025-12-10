

document.getElementById("index").addEventListener("click", () => {
    const b1 = document.getElementById("postButton");
    const b2 = document.getElementById("foundButton");

    if (b1.innerHTML != "Found") {
        [b1.innerHTML, b2.innerHTML] = [b2.innerHTML, b1.innerHTML];
        [b1.value, b2.value] = [b2.value, b1.value];
    }
})

document.getElementById("postButton").addEventListener("click", () => {
    

    const b1 = document.getElementById("postButton");
    const b2 = document.getElementById("foundButton");


    loadItem(b1.value);

    [b1.innerHTML, b2.innerHTML] = [b2.innerHTML, b1.innerHTML];
    [b1.value, b2.value] = [b2.value, b1.value];


});


function loadItem(type) {
    fetch(`/Home/updateCard?category=${type}`)
        .then(response => response.text())
        .then(html => {
            document.querySelector(".cardparent").innerHTML = html;
        })
        .catch(err => console.error(err));
}

document.getElementById("filter").addEventListener("click", () => {

    const b1 = document.getElementById("foundButton");


    const startDate = document.getElementById("startDate").value;
    const endDate = document.getElementById("endDate").value;

    let category;
    if (b1.value == "FOUNDITEM") {
        category = "FOUNDITEM";
    } else {
        category = "LOSTITEM";
    }
    filterCard(category, startDate, endDate);
    
})

function filterCard(category, startDate, endDate) {

    fetch(`/Home/filter?category=${category}&startDate=${startDate}&endDate=${endDate}`)
        .then(response => response.text())
        .then(html => document.querySelector(".cardparent").innerHTML = html)

}