document.getElementById("postButton").addEventListener("click", () => {
    

    const b1 = document.getElementById("postButton");
    const b2 = document.getElementById("foundButton");


    [b1.innerHTML, b2.innerHTML] = [b2.innerHTML, b1.innerHTML];

    loadItem(b1.innerHTML);


});


function loadItem(type) {

    fetch(`/Home/updateCard?category=${type}`)
        .then(response => (response.innerHTML)
            .then(html => {
                document.querySelector("right-container-bottom").innerHTML = html;
            })

}