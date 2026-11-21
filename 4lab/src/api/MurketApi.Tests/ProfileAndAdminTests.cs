using MurketApi.Application.Dtos;
using MurketApi.Domain.Catalog;
using MurketApi.Domain.Common;
using MurketApi.Infrastructure.Seeding;

namespace MurketApi.Tests;

/// <summary>Личный кабинет и модуль менеджера: конфликты, лимиты, CRUD (реальная PostgreSQL).</summary>
public sealed class ProfileAndAdminTests
{
    [Fact]
    public async Task Профиль_ОбновлениеИКонфликтыУникальности()
    {
        await using var host = await TestHost.CreateAsync();

        var updated = await host.Profile.UpdateProfileAsync(TestHost.CustomerId,
            new UpdateProfileDto("Анна Кошкина", "anna.k@example.com", null));
        Assert.Equal("Анна Кошкина", updated.FullName);

        // Email занят админом.
        var ex = await Assert.ThrowsAsync<ConflictException>(() => host.Profile.UpdateProfileAsync(
            TestHost.CustomerId, new UpdateProfileDto("Анна", "admin@murket.ru", null)));
        Assert.Equal("Пользователь с таким email уже существует", ex.Message);
    }

    [Fact]
    public async Task Профиль_НекорректныйEmailИТелефон_400ПоПолям()
    {
        await using var host = await TestHost.CreateAsync();

        var ex = await Assert.ThrowsAsync<ValidationException>(() => host.Profile.UpdateProfileAsync(
            TestHost.CustomerId, new UpdateProfileDto("", "не-email", "+7123")));
        Assert.Contains("fullName", ex.Errors.Keys);
        Assert.Contains("email", ex.Errors.Keys);
        Assert.Contains("phone", ex.Errors.Keys);
    }

    [Fact]
    public async Task СменаПароля_НеверныйТекущий_400()
    {
        await using var host = await TestHost.CreateAsync();

        var ex = await Assert.ThrowsAsync<ValidationException>(() => host.Profile.ChangePasswordAsync(
            TestHost.CustomerId, new ChangePasswordDto("wrong!", "НовыйПароль7!"), null));
        Assert.Equal("Неверный текущий пароль", ex.Message);
    }

    [Fact]
    public async Task СменаПароля_УспехИОтзывПрочихСессий()
    {
        await using var host = await TestHost.CreateAsync();
        var session1 = (await host.Auth.LoginAsync("customer", "customer123", "ip1")).RefreshToken;
        var session2 = (await host.Auth.LoginAsync("customer", "customer123", "ip2")).RefreshToken;

        // Меняем пароль из сессии 1: её refresh передаём как текущий.
        await host.Profile.ChangePasswordAsync(TestHost.CustomerId,
            new ChangePasswordDto("customer123", "НовыйПароль7!"), session1);

        // Сессия 2 отозвана, сессия 1 жива.
        await Assert.ThrowsAsync<UnauthorizedException>(() => host.Auth.RefreshAsync(session2));
        Assert.NotNull(await host.Auth.RefreshAsync(session1));
    }

    [Fact]
    public async Task Адреса_СозданиеОграничениеИЕдинственныйПоУмолчанию()
    {
        await using var host = await TestHost.CreateAsync();

        var created = await host.Profile.CreateAddressAsync(TestHost.CustomerId, new AddressDraftDto(
            "Дача", "Анна Смирнова", "+79991234567", "Звенигород", "ул. Лесная", "1",
            null, null, null, null, null, IsDefault: true));
        Assert.True(created.IsDefault);

        // Прежний адрес по умолчанию сброшен.
        Assert.DoesNotContain(await host.Profile.AddressesAsync(TestHost.CustomerId),
            address => address.Id == DemoData.HomeAddressId && address.IsDefault);
    }

    [Fact]
    public async Task Адреса_Лимит10_409()
    {
        await using var host = await TestHost.CreateAsync(seedDemoData: false);

        for (var i = 0; i < 10; i++)
        {
            await host.Profile.CreateAddressAsync(TestHost.CustomerId, new AddressDraftDto(
                null, "Анна Смирнова", "+79991234567", "Москва", "ул. Новая", $"{i}",
                null, null, null, null, null, IsDefault: i == 0));
        }

        await Assert.ThrowsAsync<ConflictException>(() => host.Profile.CreateAddressAsync(
            TestHost.CustomerId, new AddressDraftDto(
                null, "Анна Смирнова", "+79991234567", "Москва", "ул. Лишняя", "99",
                null, null, null, null, null, false)));
    }

    [Fact]
    public async Task Адреса_ЧужойИОтсутствующий_404()
    {
        await using var host = await TestHost.CreateAsync();

        await Assert.ThrowsAsync<NotFoundException>(
            () => host.Profile.UpdateAddressAsync(TestHost.AdminId, DemoData.HomeAddressId,
                new AddressDraftDto(
                    null, "Админ", "+79991234567", "Москва", "ул. Лесная", "7",
                    null, null, null, null, null, false)));
    }

    [Fact]
    public async Task Товары_ДубликатАртикула_409_СозданиеИУдаление()
    {
        await using var host = await TestHost.CreateAsync();

        var draft = new ProductDraftDto("RC-777", "Новый корм", "Описание", DemoData.FoodCategoryId,
            DemoData.TriolBrandId, 100m, 5, "assets/img/products/korm.svg", true);
        var created = await host.Admin.CreateProductAsync(draft);
        Assert.Equal("RC-777", created.Sku);

        await Assert.ThrowsAsync<ConflictException>(() => host.Admin.CreateProductAsync(draft));
        await Assert.ThrowsAsync<ConflictException>(() => host.Admin.UpdateProductAsync(
            DemoData.ProductId("RC-001"), new ProductDraftDto("RC-777", "Конфликт", "",
                DemoData.FoodCategoryId, null, 1m, 1, "img", true)));

        await host.Admin.DeleteProductAsync(created.Id);
        await Assert.ThrowsAsync<NotFoundException>(() => host.Admin.UpdateProductAsync(created.Id, draft));
    }

    [Fact]
    public async Task Товары_НекорректнаяКатегория_400()
    {
        await using var host = await TestHost.CreateAsync();

        var ex = await Assert.ThrowsAsync<ValidationException>(() => host.Admin.CreateProductAsync(
            new ProductDraftDto("XX-001", "Товар", "", "c-нет", null, 10m, 1, "img", true)));
        Assert.Contains("categoryId", ex.Errors.Keys);
    }

    [Fact]
    public async Task Товары_ПоискИПагинация_ВключаяНеактивные()
    {
        await using var host = await TestHost.CreateAsync();

        var page1 = await host.Admin.ProductsAsync(null, new PageRequest(1, 10));
        Assert.Equal(41, page1.Total); // все товары, включая 2 неактивных
        Assert.Equal(10, page1.Items.Count);

        var found = await host.Admin.ProductsAsync("rc-00", new PageRequest(1, 48));
        Assert.All(found.Items, product => product.Sku.StartsWith("RC-00"));
        Assert.Equal(5, found.Total); // RC-001..RC-005
    }

    [Fact]
    public async Task Пользователи_СписокТолькоЧтение()
    {
        await using var host = await TestHost.CreateAsync();

        var users = await host.Admin.UsersAsync();
        Assert.Equal(2, users.Count);
        Assert.Contains(users, user => user.Login == "admin" && user.Role == "admin");
    }

    [Fact]
    public async Task Каталог_ТолькоАктивные_ФильтрыСортировкаПагинация()
    {
        await using var host = await TestHost.CreateAsync();

        // Активных 39 из 41.
        Assert.Equal(39,
            (await host.Catalog.ProductsAsync(new CatalogQuery { Page = new PageRequest(1, 48) })).Total);

        // Категория «Игрушки» — 7 активных.
        var toys = await host.Catalog.ProductsAsync(new CatalogQuery
        {
            CategoryId = DemoData.ToysCategoryId,
            Page = new PageRequest(1, 48),
        });
        Assert.Equal(7, toys.Total);

        // Цена 500–1000: только товары в диапазоне.
        var priced = await host.Catalog.ProductsAsync(new CatalogQuery
        {
            PriceMin = 500,
            PriceMax = 1000,
            Page = new PageRequest(1, 48),
        });
        Assert.All(priced.Items, product => Assert.True(product.Price >= 500 && product.Price <= 1000));

        // Сортировка по цене возрастает.
        var sorted = await host.Catalog.ProductsAsync(new CatalogQuery
        {
            Sort = CatalogSort.PriceAsc,
            Page = new PageRequest(1, 48),
        });
        Assert.Equal(sorted.Items.Select(product => product.Price).OrderBy(price => price),
            sorted.Items.Select(product => product.Price));

        // Пагинация: страница 2 по 12.
        var page2 = await host.Catalog.ProductsAsync(new CatalogQuery { Page = new PageRequest(2, 12) });
        Assert.Equal(12, page2.Items.Count);
        Assert.Equal(39, page2.Total);
    }

    [Fact]
    public async Task Каталог_КатегорииИБрендыСКоличествами()
    {
        await using var host = await TestHost.CreateAsync();

        var categories = await host.Catalog.CategoriesAsync();
        Assert.Equal(5, categories.Count);
        Assert.Equal(12, categories.Single(category => category.Id == DemoData.FoodCategoryId).Count);

        // Бренды в категории «Игрушки» — только Triol.
        var brands = await host.Catalog.BrandsAsync(DemoData.ToysCategoryId);
        var brand = Assert.Single(brands);
        Assert.Equal((DemoData.TriolBrandId, 7), (brand.Id, brand.Count));
    }

    [Fact]
    public async Task Каталог_КарточкаНеактивного_404()
    {
        await using var host = await TestHost.CreateAsync();

        await Assert.ThrowsAsync<NotFoundException>(
            () => host.Catalog.ProductAsync(DemoData.ProductId("RC-005")));
        Assert.NotNull(await host.Catalog.ProductAsync(DemoData.ProductId("RC-001")));
    }

    [Fact]
    public async Task Каталог_ПоискПоНазваниюАртикулуБренду()
    {
        await using var host = await TestHost.CreateAsync();

        var bySku = await host.Catalog.ProductsAsync(
            new CatalogQuery { Search = "rc-001", Page = new PageRequest(1, 48) });
        Assert.Single(bySku.Items);

        var byBrand = await host.Catalog.ProductsAsync(
            new CatalogQuery { Search = "grandorf", Page = new PageRequest(1, 48) });
        Assert.Equal(3, byBrand.Total);
    }
}
