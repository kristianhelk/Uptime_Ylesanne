
//Tabeli täitmine tulemustega
function ListItems (model) {
    var i = 0;
    console.log(model);
    $("#itemlist").find("tr").each(function () {
        $(this).append("<td><a href='" + model.AmazonItemList[i].Url + "'>" + model.AmazonItemList[i].Nimi + "</a>" + "</td>");
        $(this).append("<td class='price'>" + model.AmazonItemList[i].Hind + "</td>");
        i++;
    });
};

//Järgmise lehekülje näitamine ning ülejärgmise jaoks täiendavalt amazoni poole pöördumine
function NextPage(itemList, prices, pageCount, url) {
    var i = pageCount.count * 13;
    $("#itemlist").find("tr").each(function () {
        if (i < 50) {
            $(this).html("<td><a href='" + itemList.list.AmazonItemList[i].Url + "'>" + itemList.list.AmazonItemList[i].Nimi + "</a>" + "</td>");
            $(this).append("<td class='price'>" + prices.list[i] + "</td>");
        } else {
            $(this).html("");
        }
        i++;
    });
    //Esemete nimekirja täiendamine
    if (pageCount.count < 3) {
        $.ajax({
            url: url,
            type: 'POST',
            dataType: 'json',
            cache: false,
            data: { pageCount: pageCount.count, returnedModel: itemList.list },
            success: function (result) {
                var firstNew = Object.keys(itemList.list.AmazonItemList).length;
                itemList.list = result.model;
                for (var j = firstNew; j < Object.keys(itemList.list.AmazonItemList).length; j++) {
                    if (prices.cur != "USD") {
                        if (itemList.list.AmazonItemList[j].Hind == "--") {
                            prices.list.push("--");
                        } else {
                            var p = parseFloat(itemList.list.AmazonItemList[j].Hind.slice(1));
                            var newPrice = p * prices.rate;
                            var newPriceRound = newPrice.toFixed(2).toString();
                            switch (prices.cur) {
                                case "EUR":
                                    var symbol = "€";
                                    break;
                                case "USD":
                                    var symbol = "$";
                                    break;
                                case "GBP":
                                    var symbol = "£";
                                    break;
                                default:
                                    var symbol = " ";
                            }
                            prices.list.push(symbol + newPriceRound);
                        }
                    } else {
                        prices.list.push(itemList.list.AmazonItemList[j].Hind);
                    }
                }
                console.log(prices);
            }
        });
    } else {
        $("#next").prop("disabled", true);
    }
    pageCount.count++;
    $("#prev").prop("disabled", false);
};

//Eelmisele leheküljele naasmine
function PrevPage(list, prices, pageCount) {
    var i = (pageCount.count - 2) * 13;
    $("tr").each(function () {
        $(this).html("<td><a href='" + list.AmazonItemList[i].Url + "'>" + list.AmazonItemList[i].Nimi + "</a>" + "</td>");
        $(this).append("<td>" + prices[i] + "</td>");
        i++;
    });
    pageCount.count--;
    if (pageCount.count == 1) {
        $("#prev").prop("disabled", true);
    }
    $("#next").prop("disabled", false);
}

//Kõigi võimalike kursside küsimine
function GetCurrencies(url) {
    $.ajax({
        url: 'https://openexchangerates.org/api/currencies.json?app_id=[app_id]',
        type: 'GET',
        dataType: 'json',
        success: function (result) {
            var curList = result;
            for (var cur in curList) {
                $("#currency").append("<option>" + cur.toString() + "</option>");
            }
            if ($("#currency option:contains('USD')") != null) {
                $("#currency option:contains('USD')").prop("selected", true);
            }
        }
    })
}

//Hindade muutmine valuutakursi muutmisel
function ChangePrices(list, pageCount, val, prices) {
    $.ajax({
        url: 'https://openexchangerates.org/api/latest.json?app_id=[app_id]',
        type: 'GET',
        dataType: 'json',
        cache: false,
        success: function (result) {
            var pricesFloat = [];
            prices.rate = result.rates[val];
            $(list.AmazonItemList).each(function () {
                if (this.Hind == "--") {
                    pricesFloat.push(-1);
                } else {
                    var p = this.Hind.slice(1);
                    pricesFloat.push(parseFloat(p));
                }
            });
            var i = 0;
            $(pricesFloat).each(function () {
                if (this == -1) {
                    prices.list[i] = "--";
                } else {
                    var newPrice = this * prices.rate;
                    var newPriceRound = newPrice.toFixed(2).toString();
                    switch (val) {
                        case "EUR":
                            var symbol = "€";
                            break;
                        case "USD":
                            var symbol = "$";
                            break;
                        case "GBP":
                            var symbol = "£";
                            break;
                        default:
                            var symbol = " ";
                    }
                    prices.list[i] = symbol + newPriceRound;
                }
                i++;
            });
            i = (pageCount - 1) * 13;
            $("#itemlist").find("td:last-child").each(function () {
                $(this).html(prices.list[i]);
                i++;
            });
            prices.cur = val;
        }
    });
}