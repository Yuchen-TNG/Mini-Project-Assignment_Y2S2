document.getElementById("postButton").addEventListener("click", () => {
    

    const b1 = document.getElementById("postButton");
    const b2 = document.getElementById("foundButton");


    [b1.innerHTML, b2.innerHTML] = [b2.innerHTML, b1.innerHTML];
    [b1.value, b2.value] = [b2.value, b1.value];

    loadItem(b1.value);


});


function loadItem(type) {
    fetch(`/Home/updateCard?category=${type}`)
        .then(response => response.text())
        .then(html => {
            document.querySelector(".cardparent").innerHTML = html;
        })
        .catch(err => console.error(err));
}

