﻿using System.Collections.Generic;
using DataLayer.EfClasses;

namespace BizLogic;

public interface IGeneratePurchaseItemsForRecipesAction
{
    IEnumerable<PurchaseItem> GeneratePurchaseItems(IEnumerable<Recipe> recipes);
}