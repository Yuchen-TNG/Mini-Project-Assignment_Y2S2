
const back = document.getElementById("pagingBack");
const next = document.getElementById("pagingNext");

back.disabled = true;

async function checkTotal() {
    const response = await fetch(`/Home/totalItem`);
    const data = await response.json();
    return Math.ceil(data.totalCount / 10); // 向上取整
}

let currentPaging = localStorage.getItem("currentPaging")
    ? parseInt(localStorage.getItem("currentPaging"))
    : 1;

function refresh(){
    const b1 = document.getElementById("postButton");
    const b2 = document.getElementById("foundButton");

    if (b1.innerHTML != "Found") {
        [b1.innerHTML, b2.innerHTML] = [b2.innerHTML, b1.innerHTML];
        [b1.value, b2.value] = [b2.value, b1.value];
    }

    document.getElementById("currentPage").innerHTML = 1;
    back.disabled = true;
    next.disabled = false;

    currentPaging = 1
    localStorage.setItem("currentPaging", 1);
    loadPage(currentPaging, null, null, null);
}

document.getElementById("index").addEventListener("click", () => {
    refresh()
})

document.getElementById("postButton").addEventListener("click", () => {
    

    const b1 = document.getElementById("postButton");
    const b2 = document.getElementById("foundButton");

    document.getElementById("currentPage").innerHTML = 1;
    back.disabled = true;
    next.disabled = false;

    [b1.innerHTML, b2.innerHTML] = [b2.innerHTML, b1.innerHTML];
    [b1.value, b2.value] = [b2.value, b1.value];
    currentPaging = 1
    localStorage.setItem("currentPaging", 1);
    filterCard(b2.value,null,null,null);
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
    currentPaging = 1
    localStorage.setItem("currentPaging", 1);
    filterCard(category, startDate, endDate,locationID);
    
})

async function filterCard(category, startDate, endDate, locationID) {

    // 1️⃣ fetch filter 结果
    const response = await fetch(`/Home/filter?category=${category}&startDate=${startDate}&endDate=${endDate}&locationID=${locationID}`);
    const html = await response.text();
    document.querySelector(".cardparent").innerHTML = html;

    // 2️⃣ 页码重置
    currentPaging = 1;
    localStorage.setItem("currentPaging", 1);
    document.getElementById("currentPage").innerHTML = 1;

    // 3️⃣ 获取总页数（确保按钮状态正确）
    const total = await checkTotal();

    // 4️⃣ 根据总页数设置按钮状态
    back.disabled = true;           // 一定是第一页
    next.disabled = total <= 1;     // 如果只有一页，禁用 next
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


//paging


document.getElementById("pagingNext").addEventListener("click", async () => {
    const total = await checkTotal();
    if (currentPaging < total) {
        currentPaging++;
        loadPage(currentPaging);
        
    } else {
        
       return;
    }
});



document.getElementById("pagingBack").addEventListener("click", async () => {
    const total = await checkTotal();
    if (currentPaging > 1) {   // 上一页逻辑判断
        currentPaging--;
        loadPage(currentPaging);
        
    } else {
        
        return;
    }

});


async function loadPage(page) {
    const size = 10;
    const total = await checkTotal();

    fetch(`/Home/IndexPaging?size=${size}&page=${page}`)
        .then(res => res.text())
        .then(html => {
            document.querySelector(".cardparent").innerHTML = html;
            document.getElementById("currentPage").innerHTML = page;
        });
    currentPaging = page;
    localStorage.setItem("currentPaging", page); // 保存到 localStorage

    document.getElementById("pagingBack").disabled = page <= 1;
    document.getElementById("pagingNext").disabled = page >= total;
}

document.getElementById("resetFilter").addEventListener("click", () => {
    // 1️⃣ 重置 Location
    document.getElementById("location").value = "";

    // 2️⃣ 重置日期
    document.getElementById("startDate").value = "";
    document.getElementById("endDate").value = "";

    // 3️⃣ 页码回到 1
    currentPaging = 1;
    localStorage.setItem("currentPaging", 1);

    // 4️⃣ 直接加载第一页（等于“清空筛选”）
    loadPage(1);
});