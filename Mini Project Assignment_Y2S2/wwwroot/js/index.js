

document.getElementById("index").addEventListener("click", () => {
    const b1 = document.getElementById("postButton");
    const b2 = document.getElementById("foundButton");

    if (b1.innerHTML != "Found") {
        [b1.innerHTML, b2.innerHTML] = [b2.innerHTML, b1.innerHTML];
        [b1.value, b2.value] = [b2.value, b1.value];
    }

    loadPage(currentPaging);
})

document.getElementById("postButton").addEventListener("click", () => {
    

    const b1 = document.getElementById("postButton");
    const b2 = document.getElementById("foundButton");


    loadItem(b1.value);

    [b1.innerHTML, b2.innerHTML] = [b2.innerHTML, b1.innerHTML];
    [b1.value, b2.value] = [b2.value, b1.value];

    loadPage(currentPaging);
});

const temp = document.getElementById("tempdata");
if (temp) {
    setTimeout(() => {
        temp.classList.add("hide");
        setTimeout(() => {
            temp.style.display = "none";
        }, 1000);
    }, 3000);
}

function loadItem(type) {
    fetch(`/Home/updateCard?category=${type}`)
        .then(response => response.text())
      
        .catch(err => console.error(err));
}

document.getElementById("filter").addEventListener("click", () => {

    const b1 = document.getElementById("foundButton");


    const startDate = document.getElementById("startDate").value;
    const endDate = document.getElementById("endDate").value;
    const locationID = document.getElementById("location").value;

    let category;
    if (b1.value == "FOUNDITEM") {
        category = "FOUNDITEM";
    } else {
        category = "LOSTITEM";
    }
    filterCard(category, startDate, endDate,locationID);
    
})

function filterCard(category, startDate, endDate,locationID) {

    fetch(`/Home/filter?category=${category}&startDate=${startDate}&endDate=${endDate}&locationID=${locationID}`)
        .then(response => response.text())
        .then(html => document.querySelector(".cardparent").innerHTML = html);

}

const startDate = document.getElementById("startDate");
const endDate = document.getElementById("endDate");


startDate.addEventListener("change", () => {
    endDate.min = startDate.value;


    if (endDate.value < startDate.value) {
        endDate.value = startDate.value;
    }
});


endDate.addEventListener("change", () => {
    if (endDate.value < startDate.value) {
        endDate.value = startDate.value;
    }
});

let currentPaging = 1; 
//paging
document.getElementById("pagingNext").addEventListener("click", () => {
    loadPage(currentPaging + 1);
})

document.getElementById("pagingBack").addEventListener("click", () => {
    if (currentPaging > 1)
        loadPage(currentPaging - 1)
})

function loadPage(page) {
    const size = 10;
    
    fetch(`/Home/IndexPaging?size=${size}&page=${page}`)
        .then(res => res.text())
        .then(html => {
            document.querySelector(".cardparent").innerHTML = html;
            document.querySelector(".currentPage").textContent = page;
        });
    currentPaging = page;

}

loadPage(1);